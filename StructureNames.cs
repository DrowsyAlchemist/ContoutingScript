namespace Contouring
{
    public static class StructureNames
    {
        public static string[] OrgansForPrv { get; private set; } = { "SpinalCord" };
        public static string Body { get; private set; } = "BODY";
        public static string Bones { get; private set; } = "Bones";
        public static string Shoulder { get; private set; } = "xShoulder";
        public static string CtvAll { get; private set; } = "CTV_all";
        public static string PtvAll { get; private set; } = "PTV_all";
        public static string PtvOpt { get; private set; } = "PTV_Opt";
        public static string PtvT { get; private set; } = "PTV_t";
        public static string Ring { get; private set; } = "Ring";
        public static string External { get; private set; } = "External";
        public static string BodyMinusPtv { get; private set; } = "b0dy-PTV";
        public static string PtvOptMinus { get; private set; } = "PTV_Opt-1";
        public static string SpinalCord { get; private set; } = "SpinalCord";
        public static string SupportivePrefix { get; private set; } = "x";
        public static string CropPostfix { get; private set; } = "-PTV";
        public static string OptPostfix { get; private set; } = "_Opt";
        public static string PrvPostfix { get; private set; } = " PRV";
    }
}
