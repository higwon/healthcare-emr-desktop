using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HealthNote.Desktop.Preview;

namespace HealthNote.Desktop.ViewModels
{
    public sealed class TimelinePreviewViewModel : ObservableObject
    {
        private string _type = "전체";
        private string _search = string.Empty;
        private string _period = "최근 3개월";
        private string _state = "정상";
        private bool _longTitle;
        private bool _detailOpen;
        private RecordPreview? _selected;
        private IReadOnlyList<RecordPreview> _records = ExplorationFixture.Records();

        public TimelinePreviewViewModel()
        {
            CloseDetailCommand = new RelayCommand(() => DetailOpen = false);
            RetryCommand = new RelayCommand(() => State = "정상");
            CancelCommand = new RelayCommand(() => State = "조회 전 / 취소");
        }

        public IReadOnlyList<string> Types { get; } = new[] { "전체", "체성분", "검사", "검진", "증상", "복약", "방문" };
        public IReadOnlyList<string> States { get; } = new[] { "정상", "기록 없음", "조회 중", "조회 실패", "이전 조건의 자료", "조회 전 / 취소" };
        public IRelayCommand CloseDetailCommand { get; }
        public IRelayCommand RetryCommand { get; }
        public IRelayCommand CancelCommand { get; }
        public IReadOnlyList<RecordPreview> Records => _records;
        public bool HasRecords => Records.Count > 0;
        public bool HasNotice => State != "정상" || !HasRecords;
        public bool IsError => State == "조회 실패";
        public bool IsLoading => State == "조회 중";
        public string ResultContext => State == "이전 조건의 자료" ? "표시 자료: 이전 최근 3개월 · 현재 조건: " + Period : "표시 기간: " + Period;
        public string Notice => State switch
        {
            "기록 없음" => "이 기간에는 기록이 없어요.",
            "조회 중" => "기록을 확인하고 있어요. (합성 상태 preview)",
            "조회 실패" => "기록을 불러오지 못했어요. 다시 시도해 주세요. (합성 상태 preview)",
            "이전 조건의 자료" => "이전 조건의 자료를 표시하고 있어요. (합성 상태 preview)",
            "조회 전 / 취소" => "조회를 취소했어요. 아직 새 자료를 확인하지 않았어요.",
            _ => HasRecords ? "합성 데이터 예시 · 실제 조회/저장 없음" : "일치하는 기록이 없어요. 필터나 검색어를 바꿔 주세요."
        };

        public string Type
        {
            get => _type;
            set
            {
                if (SetProperty(ref _type, value))
                {
                    Refresh();
                }
            }
        }

        public string Search
        {
            get => _search;
            set
            {
                if (SetProperty(ref _search, value))
                {
                    Refresh();
                }
            }
        }

        public string Period
        {
            get => _period;
            set
            {
                if (SetProperty(ref _period, value))
                {
                    Refresh();
                }
            }
        }

        public string State
        {
            get => _state;
            set
            {
                if (SetProperty(ref _state, value))
                {
                    Refresh();
                }
            }
        }

        public bool LongTitle
        {
            get => _longTitle;
            set
            {
                if (SetProperty(ref _longTitle, value))
                {
                    Refresh();
                }
            }
        }

        public bool DetailOpen { get => _detailOpen; set => SetProperty(ref _detailOpen, value); }
        public RecordPreview? Selected
        {
            get => _selected;
            set
            {
                if (SetProperty(ref _selected, value)) DetailOpen = value != null;
            }
        }

        private void Refresh()
        {
            string? selectedId = Selected?.Id;
            bool wasOpen = DetailOpen;
            bool blocked = State != "정상" && State != "이전 조건의 자료";
            _records = blocked ? new RecordPreview[0] : ExplorationFixture.Records(LongTitle)
                .Where(r => (Type == "전체" || r.Type == Type)
                    && (State == "이전 조건의 자료" || Period != "최근 1개월" || r.Date.StartsWith("2026.09", System.StringComparison.Ordinal))
                    && (string.IsNullOrWhiteSpace(Search) || r.Title.Contains(Search) || r.Summary.Contains(Search)))
                .ToArray();
            OnPropertyChanged(nameof(Records));
            Selected = _records.FirstOrDefault(r => r.Id == selectedId);
            DetailOpen = Selected != null && wasOpen;
            OnPropertyChanged(nameof(Records));
            OnPropertyChanged(nameof(HasRecords));
            OnPropertyChanged(nameof(Notice));
            OnPropertyChanged(nameof(HasNotice));
            OnPropertyChanged(nameof(ResultContext));
            OnPropertyChanged(nameof(IsError));
            OnPropertyChanged(nameof(IsLoading));
        }
    }
}
