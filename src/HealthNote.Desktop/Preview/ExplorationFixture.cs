using System.Collections.Generic;

namespace HealthNote.Desktop.Preview
{
    internal static class ExplorationFixture
    {
        public static IReadOnlyList<RecordPreview> Records(bool longTitle = false)
        {
            return new[]
            {
                new RecordPreview("body-3", "체성분", longTitle
                    ? "정기 체성분 측정 기록 — 긴 제목에서도 측정 시각과 출처를 함께 확인할 수 있는 합성 예시"
                    : "체성분 측정", "2026.09.28", "체중 70.8 kg · 골격근량 31.2 kg"),
                new RecordPreview("symptom-2", "증상", "사용자 증상 기록", "2026.09.24", "기록한 내용과 시각을 확인해요"),
                new RecordPreview("lab-1", "검사", "혈액검사 결과", "2026.09.18", "측정 항목 4개 · 원본 출처 확인"),
                new RecordPreview("med-1", "복약", "복약 기록", "2026.09.16", "일정에 연결된 복용 기록"),
                new RecordPreview("check-1", "검진", "정기 건강검진", "2026.09.10", "검진 기록의 요약과 출처"),
                new RecordPreview("visit-1", "방문", "방문 기록", "2026.08.20", "일상의 건강 기록을 한곳에서 확인해요")
            };
        }

        public static IReadOnlyList<MeasurementPreview> Measurements(string metric)
        {
            bool weight = metric == "체중";
            return new[]
            {
                new MeasurementPreview("2026.09.28", weight ? 70.8m : 31.2m, "kg"),
                new MeasurementPreview("2026.09.14", weight ? 71.1m : 31.0m, "kg"),
                new MeasurementPreview("2026.09.07", null, "kg"),
                new MeasurementPreview("2026.08.31", weight ? 71.4m : 30.9m, "kg"),
                new MeasurementPreview("2026.08.17", weight ? 71.8m : 30.8m, "kg")
            };
        }
    }
}
