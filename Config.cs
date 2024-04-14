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

        public static string BodyName { get; private set; } = "BODY";
        public static string BonesName { get; private set; } = "Bones";
        public static string PRVName { get; private set; } = "PRV";
        public static string ShoulderName { get; private set; } = "Shoulder";
        public static string CtvAllName { get; private set; } = "CTV_all";
        public static string PtvAllName { get; private set; } = "PTV_all";
        public static string PtvOptName { get; private set; } = "PTV_t";
        public static string PtvTName { get; private set; } = "Shoulder";
        public static string RingName { get; private set; } = "xRing";
        public static string ExternalName { get; private set; } = "xExternal";
        public static string BodyMinusPtvName { get; private set; } = "xb0dy-PTV";
        public static string PtvOptMinusName { get; internal set; } = "PTV_Opt-1";

        public static string CropPostfix { get; private set; } = "-PTV";
        public static string CropPrefix { get; private set; } = "x";
        public static string OptPostfix { get; private set; } = "_Opt";

        public static uint CropMargin { get; private set; } = 3;
        public static double CropVolumeTrashholdInPercents { get; private set; } = 3;
        public static double PtvOptTrashholdInPercents { get; private set; } = 3;

        public static double ExternalOutMargin { get; private set; } = 2;
        public static double ExternalMarginIntoBody { get; private set; } = 1;
        public static double ExternalHU { get; private set; } = -600;

        public static uint OptMinusInnerMargin { get; private set; } = 1;
        public static uint OptMinusMarginFromCtv { get; private set; } = 0;
    }
}
