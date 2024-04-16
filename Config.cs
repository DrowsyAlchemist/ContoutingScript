namespace Contouring
{
    public static class Config
    {
        public static bool IsDebugMode { get; private set; } = true;

        public static string OrgansType { get; private set; } = "ORGAN";
        public static string CtvType { get; private set; } = "CTV";
        public static string PtvType { get; private set; } = "PTV";
        public static uint OrganFromBodyMargin { get; private set; } = 0;
        public static uint TargetFromBody { get; private set; } = 1;

        public static uint OrgansCropMarginFromPtv { get; private set; } = 3;
        public static uint ShouderMarginFromPtv { get; private set; } = 35;
        public static uint ShouderMarginFromBody { get; private set; } = 0;
        public static double CropVolumeThresholdInPercents { get; private set; } = 3;
        public static double PtvOptThresholdInPercents { get; private set; } = 3;

        public static double ExternalOutMargin { get; private set; } = 2;
        public static double ExternalMarginIntoBody { get; private set; } = 1;
        public static double ExternalHU { get; private set; } = -600;

        public static uint OptMinusInnerMargin { get; private set; } = 1;
        public static uint OptMinusMarginFromCtv { get; private set; } = 0;

        public static string PermissionDialogText { get; private set; } = " (Press Y to accept.)";
    }
}
