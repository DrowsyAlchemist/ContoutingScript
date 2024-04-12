namespace ContouringScript
{
    public class RingCreator
    {
        private uint _innerMarginInMM;

        public uint OuterMarginInMM { get; private set; }

        public void Create()
        {
            try
            {
                SetMargins();
                Structure ptv = _structureSet.GetStructure("PTV_all");
                Structure body = _structureSet.GetStructure("BODY");
                Structure ring = _structureSet.GetOrCreateStructure("xRing");
                ring.SegmentVolume = ptv.Margin(OuterMarginInMM);
                var cropper = new StructuresCropper(ptv);
                ring.SegmentVolume = cropper.Crop(ring, _innerMarginInMM);
                ring.SegmentVolume = ring.And(body);
                _application.SaveModifications();
            }
            catch (Exception e)
            {
                Logger.WriteError(e.ToString());
            }
        }

        private void SetMargins()
        {
            Console.Write("Inner Ring margin in mm: ");
            _innerMarginInMM = GetMarginFromConsole();
            Console.Write("Outer Ring margin in mm: ");
            OuterMarginInMM = GetMarginFromConsole();
        }

        private uint GetMarginFromConsole()
        {
            bool isCorrect = false;
            uint marginInMM = 0;

            while (isCorrect == false)
            {
                isCorrect = uint.TryParse(Console.ReadLine(), out marginInMM);

                if (marginInMM <= 0)
                {
                    Logger.WriteError("Margin should be positive integer.");
                    isCorrect = false;
                }
            }
            return marginInMM;
        }
    }
}