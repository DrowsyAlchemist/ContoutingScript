namespace Contouring
{
    public static class Config
    {
        public static bool UseDefaultOrgansMargin { get; private set; } = false;
        public static uint CroppedOrgansFromPtvMargin { get; private set; } = 3;

        public static bool UseShoulderCropper { get; private set; } = true;
        public static uint ShoulderFromPtvMargin { get; private set; } = 35;
        public static uint ShouderIntoBodyMargin { get; private set; } = 0;

        public static bool UseDefaultRingMargin { get; private set; } = false;
        public static uint RingInnerMargin { get; private set; } = 3;
        public static uint RingOuterMargin { get; private set; } = 13;

        public static bool OfferBodyMinusPtv { get; private set; } = true;
        public static bool CreateBodyMinusPtvByDefault { get; private set; } = false;

        public static bool OfferPtvOptMinus { get; private set; } = true;
        public static bool CreatePtvOptMinusByDefault { get; private set; } = false;

        public static uint OrgansIntoBodyMargin { get; private set; } = 0;
        public static uint CtvIntoBodyMargin { get; private set; } = 1;

        public static double CropVolumeThresholdInPercents { get; private set; } = 1.5;
        public static double PtvOptThresholdInPercents { get; private set; } = 1.5;

        public static double ExternalFromPtvMargin { get; private set; } = 2;
        public static double ExternalIntoBodyMargin { get; private set; } = 1;
        public static double ExternalHU { get; private set; } = -600;

        public static uint OptMinusInnerMargin { get; private set; } = 1;
        public static uint OptMinusMarginFromCtv { get; private set; } = 0;

        public static uint PrvMarginForHeadOrgans { get; private set; } = 3;
    }
}
