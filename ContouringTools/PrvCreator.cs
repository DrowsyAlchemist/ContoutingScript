using Contouring.Extentions;
using System;
using VMS.TPS.Common.Model.API;

namespace Contouring.Tools
{
    class PrvCreator
    {
        private readonly uint _margin;

        private StructureSet StructureSet => Program.StructureSet;
        private string SupportivePrefix => StructureNames.SupportivePrefix;
        private string PrvPostfix => StructureNames.PrvPostfix;

        public PrvCreator(uint margin)
        {
            _margin = margin;
        }

        public void Create()
        {
            Logger.WriteInfo("\tPrvCreator: Create");

            foreach (var organName in StructureNames.OrgansForPrv)
            {
                try
                {
                    Structure organForPrv = StructureSet.GetStructure(organName);
                    Structure prv = StructureSet.GetOrCreateStructure(SupportivePrefix + organName + PrvPostfix, Config.OrgansType);
                    prv.SegmentVolume = organForPrv.Margin(_margin);
                }
                catch (Exception error)
                {
                    Logger.WriteWarning($"Can not create PRV from {organName}.\n" + error);
                }
            }
        }
    }
}
