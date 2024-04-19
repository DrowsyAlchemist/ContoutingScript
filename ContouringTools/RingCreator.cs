using Contouring.Extentions;
using System;
using VMS.TPS.Common.Model.API;

namespace Contouring.Tools
{
    public class RingCreator
    {
        private const uint MaxMargin = 100;
        private readonly CroppersFactory _croppersFactory;
        private uint _innerMarginInMM;
        private uint _outerMarginInMM;

        private StructureSet StructureSet => Program.StructureSet;

        public RingCreator(CroppersFactory croppersFactory)
        {
            _croppersFactory = croppersFactory;
        }

        public void Create()
        {
            Logger.WriteInfo("\tRingCreator: Create");

            if (_innerMarginInMM == 0 && _outerMarginInMM == 0)
                throw new Exception("Margins for Ring is not set.");

            var cropperByPtv = _croppersFactory.Create(StructureNames.PtvAll);

            try
            {
                Structure ptv = StructureSet.GetStructure(StructureNames.PtvAll);
                Structure body = StructureSet.GetStructure(StructureNames.Body);
                Structure ring = StructureSet.GetOrCreateStructure(StructureNames.SupportivePrefix + StructureNames.Ring);
                ring.SegmentVolume = ptv.Margin(_outerMarginInMM);
                ring.SegmentVolume = cropperByPtv.Crop(ring, _innerMarginInMM);
                ring.SegmentVolume = ring.And(body);
            }
            catch (Exception e)
            {
                Logger.WriteError(e.ToString());
            }
        }

        public void SetMargins(out uint inner, out uint outer)
        {
            if (Config.UseDefaultRingMargin)
            {
                inner = Config.RingInnerMargin;
                outer = Config.RingOuterMargin;
                _innerMarginInMM = inner;
                _outerMarginInMM = outer;
                return;
            }
            Console.Write("Inner Ring margin in mm: ");
            inner = GetMarginFromConsole();
            Console.Write("Outer Ring margin in mm: ");
            outer = GetMarginFromConsole();
            _innerMarginInMM = inner;
            _outerMarginInMM = outer;

            if (outer < inner)
            {
                Logger.WriteError("Outer margin should be greater than inner.");
                SetMargins(out inner,out outer);
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
