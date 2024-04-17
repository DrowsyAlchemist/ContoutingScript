namespace Contouring
{
    public static class Config
    {
        public static bool IsDebugMode { get; private set; } = true;

        public static string OrgansType { get; private set; } = "ORGAN";
        public static string CtvType { get; private set; } = "CTV";
        public static string PtvType { get; private set; } = "PTV";
        public static uint OrgansIntoBodyMargin { get; private set; } = 0;
        public static uint CtvIntoBodyMargin { get; private set; } = 1;

        public static uint CroppedOrgansFromPtvMargin { get; private set; } = 3;
        public static uint ShoulderFromPtvMargin { get; private set; } = 35;
        public static uint ShouderIntoBodyMargin { get; private set; } = 0;
        public static double CropVolumeThresholdInPercents { get; private set; } = 3;
        public static double PtvOptThresholdInPercents { get; private set; } = 3;

        public static double ExternalFromBodyMargin { get; private set; } = 2;
        public static double ExternalIntoBodyMargin { get; private set; } = 1;
        public static double ExternalHU { get; private set; } = -600;

        public static uint OptMinusInnerMargin { get; private set; } = 1;
        public static uint OptMinusMarginFromCtv { get; private set; } = 0;
    }
}
