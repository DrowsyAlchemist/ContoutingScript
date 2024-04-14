using System;
using VMS.TPS.Common.Model.API;

namespace Contouring
{
    public class RingCreator
    {
        private uint _innerMarginInMM;

        public uint OuterMarginInMM { get; private set; }
        private Application Application => Program.Application;
        private StructureSet StructureSet => Program.StructureSet;

        public RingCreator()
        {
        }

        public void Create()
        {
            Logger.WriteInfo("\tRingCreator: Create");

            try
            {
                SetMargins();
                Structure ptv = StructureSet.GetStructure(Config.PtvAllName);
                Structure body = StructureSet.GetStructure(Config.BodyName);
                Structure ring = StructureSet.GetOrCreateStructure(Config.RingName);
                var cropperByPtv = new StructuresCropper(ptv);
                ring.SegmentVolume = ptv.Margin(OuterMarginInMM);
                ring.SegmentVolume = cropperByPtv.Crop(ring, _innerMarginInMM);
                ring.SegmentVolume = ring.And(body);
                Application.SaveModifications();
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
