using System.Globalization;

namespace HealthNote.Desktop.Preview
{
    public sealed class MeasurementPreview
    {
        public MeasurementPreview(string date, decimal? value, string unit)
        {
            Date = date;
            Value = value;
            Unit = unit;
        }

        public string Date { get; }
        public decimal? Value { get; }
        public string Unit { get; }
        public string Source => "체성분 측정 · 합성 데모";
        public string TableValue => Value?.ToString("0.0", CultureInfo.InvariantCulture) ?? "결측";
        public string DisplayValue => Value.HasValue ? TableValue + " " + Unit : "측정값 없음";
        public string AccessibleName => Date + " " + DisplayValue;
    }
}
