using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HealthNote.Application.Emr;
using HealthNote.Domain.Emr;
using HealthNote.Desktop.Threading;

namespace HealthNote.Desktop.ViewModels
{
    public sealed class EmrWorkspaceViewModel : ObservableObject, IDisposable
    {
        private const int PageSize = 20;
        private readonly IEmrQuery _query;
        private readonly IUiDispatcher _dispatcher;
        private CancellationTokenSource? _patientRequest, _historyRequest, _detailRequest;
        private long _patientGeneration, _historyGeneration, _detailGeneration;
        private bool _disposed, _initialized;
        private string _search = string.Empty, _appliedSearch = string.Empty;
        private bool _patientsBusy, _historyBusy, _detailBusy;
        private string _patientsStatus = "환자를 검색해 주세요.", _historyStatus = "환자를 선택해 주세요.", _detailStatus = "진료 기록을 선택해 주세요.";
        private IReadOnlyList<Patient> _patients = Array.Empty<Patient>();
        private IReadOnlyList<EncounterSummary> _history = Array.Empty<EncounterSummary>();
        private Patient? _selectedPatient;
        private EncounterSummary? _selectedRecord;
        private EncounterNote? _detail;
        private int _patientPage = 1, _historyPage = 1;
        private long _patientCount, _historyCount;

        public EmrWorkspaceViewModel(IEmrQuery query, IUiDispatcher dispatcher)
        {
            _query = query;
            _dispatcher = dispatcher;
            SearchCommand = new AsyncRelayCommand(() => LoadPatientsAsync(Search.Trim(), 1), () => !_disposed,
                AsyncRelayCommandOptions.AllowConcurrentExecutions);
            RetryPatientsCommand = new AsyncRelayCommand(() => LoadPatientsAsync(_appliedSearch, _patientPage), () => !_disposed,
                AsyncRelayCommandOptions.AllowConcurrentExecutions);
            PreviousPatientsCommand = new AsyncRelayCommand(() => LoadPatientsAsync(_appliedSearch, _patientPage - 1), () => !_disposed && !PatientsBusy && _patientPage > 1);
            NextPatientsCommand = new AsyncRelayCommand(() => LoadPatientsAsync(_appliedSearch, _patientPage + 1), () => !_disposed && !PatientsBusy && (long)_patientPage * PageSize < _patientCount);
            LoadHistoryCommand = new AsyncRelayCommand(() => LoadHistoryAsync(1), () => !_disposed && SelectedPatient != null,
                AsyncRelayCommandOptions.AllowConcurrentExecutions);
            RetryHistoryCommand = new AsyncRelayCommand(() => LoadHistoryAsync(_historyPage), () => !_disposed && SelectedPatient != null,
                AsyncRelayCommandOptions.AllowConcurrentExecutions);
            PreviousHistoryCommand = new AsyncRelayCommand(() => LoadHistoryAsync(_historyPage - 1), () => !_disposed && !HistoryBusy && SelectedPatient != null && _historyPage > 1);
            NextHistoryCommand = new AsyncRelayCommand(() => LoadHistoryAsync(_historyPage + 1), () => !_disposed && !HistoryBusy && SelectedPatient != null && (long)_historyPage * PageSize < _historyCount);
            LoadDetailCommand = new AsyncRelayCommand(LoadDetailAsync, () => !_disposed && SelectedRecord != null,
                AsyncRelayCommandOptions.AllowConcurrentExecutions);
            CancelCommand = new RelayCommand(CancelAll, () => !_disposed && (PatientsBusy || HistoryBusy || DetailBusy));
        }

        public IAsyncRelayCommand SearchCommand { get; }
        public IAsyncRelayCommand RetryPatientsCommand { get; }
        public IAsyncRelayCommand PreviousPatientsCommand { get; }
        public IAsyncRelayCommand NextPatientsCommand { get; }
        public IAsyncRelayCommand LoadHistoryCommand { get; }
        public IAsyncRelayCommand RetryHistoryCommand { get; }
        public IAsyncRelayCommand PreviousHistoryCommand { get; }
        public IAsyncRelayCommand NextHistoryCommand { get; }
        public IAsyncRelayCommand LoadDetailCommand { get; }
        public IRelayCommand CancelCommand { get; }
        public string Search { get => _search; set => SetProperty(ref _search, value); }
        public IReadOnlyList<Patient> Patients { get => _patients; private set => SetProperty(ref _patients, value); }
        public IReadOnlyList<EncounterSummary> History { get => _history; private set => SetProperty(ref _history, value); }
        public EncounterNote? Detail { get => _detail; private set => SetProperty(ref _detail, value); }
        public string PatientsStatus { get => _patientsStatus; private set => SetProperty(ref _patientsStatus, value); }
        public string HistoryStatus { get => _historyStatus; private set => SetProperty(ref _historyStatus, value); }
        public string DetailStatus { get => _detailStatus; private set => SetProperty(ref _detailStatus, value); }
        public string PatientPaging => _patientPage + " 페이지 · " + _patientCount + "명";
        public string HistoryPaging => _historyPage + " 페이지 · " + _historyCount + "건";
        public string PatientContext => SelectedPatient == null ? "선택된 환자 없음" : SelectedPatient.DisplayName + " · " + SelectedPatient.PatientNumber;
        public bool PatientsBusy { get => _patientsBusy; private set { SetProperty(ref _patientsBusy, value); NotifyCommands(); } }
        public bool HistoryBusy { get => _historyBusy; private set { SetProperty(ref _historyBusy, value); NotifyCommands(); } }
        public bool DetailBusy { get => _detailBusy; private set { SetProperty(ref _detailBusy, value); NotifyCommands(); } }

        public Patient? SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                if (_disposed || !SetProperty(ref _selectedPatient, value)) return;
                ClearHistory();
                OnPropertyChanged(nameof(PatientContext));
                NotifyCommands();
                if (value != null) LoadHistoryCommand.Execute(null);
            }
        }
        public EncounterSummary? SelectedRecord
        {
            get => _selectedRecord;
            set
            {
                if (_disposed || !SetProperty(ref _selectedRecord, value)) return;
                ClearDetail();
                NotifyCommands();
                if (value != null) LoadDetailCommand.Execute(null);
            }
        }

        public Task InitializeAsync()
        {
            if (_initialized || _disposed) return Task.CompletedTask;
            _initialized = true;
            return LoadPatientsAsync(string.Empty, 1);
        }

        private async Task LoadPatientsAsync(string search, int page)
        {
            if (_disposed) return;
            if (PatientsBusy && _appliedSearch == search && _patientPage == page) return;
            ++_patientGeneration;
            _patientRequest?.Cancel();
            var request = new CancellationTokenSource();
            _patientRequest = request;
            long generation = _patientGeneration;
            _appliedSearch = search;
            _patientPage = page;
            _patientCount = 0;
            SelectedPatient = null;
            ClearHistory();
            Patients = Array.Empty<Patient>();
            PatientsBusy = true;
            PatientsStatus = "환자 조회 중…";
            OnPropertyChanged(nameof(PatientPaging));
            Page<Patient>? result = null;
            bool failed = false;
            try { result = await _query.GetPatientsAsync(search, page, PageSize, request.Token).ConfigureAwait(false); }
            catch (OperationCanceledException) when (request.IsCancellationRequested) { }
            catch (Exception) { failed = true; }
            try
            {
                await _dispatcher.ApplyAsync(() =>
                {
                    if (_disposed || generation != _patientGeneration || request.IsCancellationRequested) return;
                    _patientRequest = null;
                    if (!failed && result != null)
                    {
                        _patientCount = result.TotalCount;
                        Patients = result.Items;
                    }
                    PatientsStatus = failed ? "환자를 조회하지 못했어요. 다시 시도해 주세요."
                        : Patients.Count == 0 ? "검색 결과가 없어요." : "환자를 선택해 진료 이력을 확인하세요.";
                    OnPropertyChanged(nameof(PatientPaging));
                    PatientsBusy = false;
                }).ConfigureAwait(false);
            }
            finally { request.Dispose(); }
        }

        private async Task LoadHistoryAsync(int page)
        {
            if (_disposed || SelectedPatient == null) return;
            if (HistoryBusy && _historyPage == page) return;
            Guid patientId = SelectedPatient.Id;
            ClearHistory();
            _historyPage = page;
            var request = new CancellationTokenSource();
            _historyRequest = request;
            long generation = ++_historyGeneration;
            HistoryBusy = true;
            HistoryStatus = "진료 이력 조회 중…";
            Page<EncounterSummary>? result = null;
            bool failed = false;
            try { result = await _query.GetEncountersAsync(patientId, page, PageSize, request.Token).ConfigureAwait(false); }
            catch (OperationCanceledException) when (request.IsCancellationRequested) { }
            catch (Exception) { failed = true; }
            try
            {
                await _dispatcher.ApplyAsync(() =>
                {
                    if (_disposed || generation != _historyGeneration || request.IsCancellationRequested || SelectedPatient?.Id != patientId) return;
                    _historyRequest = null;
                    if (!failed && result != null)
                    {
                        _historyCount = result.TotalCount;
                        History = result.Items;
                    }
                    HistoryStatus = failed ? "진료 이력을 조회하지 못했어요. 다시 시도해 주세요."
                        : History.Count == 0 ? "등록된 진료 기록이 없어요." : "기록을 선택해 상세를 확인하세요.";
                    OnPropertyChanged(nameof(HistoryPaging));
                    HistoryBusy = false;
                }).ConfigureAwait(false);
            }
            finally { request.Dispose(); }
        }

        private async Task LoadDetailAsync()
        {
            if (_disposed || SelectedPatient == null || SelectedRecord == null) return;
            if (DetailBusy) return;
            Guid patientId = SelectedPatient.Id;
            Guid recordId = SelectedRecord.Id;
            ClearDetail();
            var request = new CancellationTokenSource();
            _detailRequest = request;
            long generation = ++_detailGeneration;
            DetailBusy = true;
            DetailStatus = "기록 상세 조회 중…";
            EncounterNote? result = null;
            bool failed = false;
            try { result = await _query.GetEncounterAsync(patientId, recordId, request.Token).ConfigureAwait(false); }
            catch (OperationCanceledException) when (request.IsCancellationRequested) { }
            catch (Exception) { failed = true; }
            try
            {
                await _dispatcher.ApplyAsync(() =>
                {
                    if (_disposed || generation != _detailGeneration || request.IsCancellationRequested ||
                        SelectedPatient?.Id != patientId || SelectedRecord?.Id != recordId) return;
                    _detailRequest = null;
                    Detail = result;
                    DetailStatus = failed ? "상세를 조회하지 못했어요. 다시 시도해 주세요." : "서버에 저장된 기록";
                    DetailBusy = false;
                }).ConfigureAwait(false);
            }
            finally { request.Dispose(); }
        }

        private void ClearHistory()
        {
            ++_historyGeneration;
            _historyRequest?.Cancel();
            _historyRequest = null;
            SelectedRecord = null;
            ClearDetail();
            History = Array.Empty<EncounterSummary>();
            _historyCount = 0;
            _historyPage = 1;
            HistoryStatus = "환자를 선택해 주세요.";
            HistoryBusy = false;
            OnPropertyChanged(nameof(HistoryPaging));
        }
        private void ClearDetail()
        {
            ++_detailGeneration;
            _detailRequest?.Cancel();
            _detailRequest = null;
            Detail = null;
            DetailBusy = false;
            DetailStatus = "진료 기록을 선택해 주세요.";
        }
        private void CancelAll()
        {
            ++_patientGeneration;
            _patientRequest?.Cancel();
            _patientRequest = null;
            if (PatientsBusy) PatientsStatus = "환자 조회를 취소했어요.";
            PatientsBusy = false;
            bool historyBusy = HistoryBusy;
            bool detailBusy = DetailBusy;
            ClearHistory();
            if (historyBusy) HistoryStatus = "진료 이력 조회를 취소했어요.";
            if (detailBusy) DetailStatus = "상세 조회를 취소했어요.";
        }
        private void NotifyCommands()
        {
            PreviousPatientsCommand.NotifyCanExecuteChanged();
            NextPatientsCommand.NotifyCanExecuteChanged();
            LoadHistoryCommand.NotifyCanExecuteChanged();
            RetryHistoryCommand.NotifyCanExecuteChanged();
            PreviousHistoryCommand.NotifyCanExecuteChanged();
            NextHistoryCommand.NotifyCanExecuteChanged();
            LoadDetailCommand.NotifyCanExecuteChanged();
            CancelCommand.NotifyCanExecuteChanged();
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            CancelAll();
            SearchCommand.NotifyCanExecuteChanged();
            RetryPatientsCommand.NotifyCanExecuteChanged();
            NotifyCommands();
        }
    }
}
