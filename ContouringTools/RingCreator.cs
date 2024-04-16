using Contouring.Extentions;
using System;
using VMS.TPS.Common.Model.API;

namespace Contouring.Tools
{
    public class RingCreator
    {
        private const uint MaxMargin = 100;
        private readonly StructuresCropper _cropperByPtv;
        private uint _innerMarginInMM;

        public uint OuterMarginInMM { get; private set; }
        private StructureSet StructureSet => Program.StructureSet;

        public RingCreator(CroppersFactory croppersFactory)
        {
            _cropperByPtv = croppersFactory.Create(StructureNames.PtvAll);
        }

        public void Create()
        {
            Logger.WriteInfo("\tRingCreator: Create");

            try
            {
                SetMargins();
                Structure ptv = StructureSet.GetStructure(StructureNames.PtvAll);
                Structure body = StructureSet.GetStructure(StructureNames.Body);
                Structure ring = StructureSet.GetOrCreateStructure(StructureNames.SupportivePrefix + StructureNames.Ring);
                ring.SegmentVolume = ptv.Margin(OuterMarginInMM);
                ring.SegmentVolume = _cropperByPtv.Crop(ring, _innerMarginInMM);
                ring.SegmentVolume = ring.And(body);
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

            if (OuterMarginInMM < _innerMarginInMM)
            {
                Logger.WriteError("Outer margin should be greater than inner.");
                SetMargins();
            }
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
                if (marginInMM > MaxMargin)
                {
                    Logger.WriteError($"Margin should be less then {MaxMargin} mm.");
                    isCorrect = false;
                }
            }
            return marginInMM;
        }
    }
}
