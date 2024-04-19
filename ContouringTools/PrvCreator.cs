using Contouring.Extentions;
using System;
using VMS.TPS.Common.Model.API;

namespace Contouring.Tools
{
    class PrvCreator
    {
        private readonly string[] _headOrgans = {
            StructureNames.Chiasm,
            StructureNames.BrainStem,
            StructureNames.OpticNerveL,
            StructureNames.OpticNerveR,
            StructureNames.LensL,
            StructureNames.LensR
        };

        private StructureSet StructureSet => Program.StructureSet;
        private string SupportivePrefix => StructureNames.SupportivePrefix;
        private string PrvPostfix => StructureNames.PrvPostfix;

        public PrvCreator()
        {
        }

        public void Create(uint ptvMargin)
        {
            Logger.WriteInfo("\tPrvCreator: Create");

            CreatePrvFrom(StructureNames.SpinalCord, ptvMargin);
            CreatePrvFrom(StructureNames.BrainStem, ptvMargin);

            foreach (var organName in _headOrgans)
                CreatePrvFrom(organName, Config.PrvMarginForHeadOrgans);
        }

        private void CreatePrvFrom(string organName, uint margin)
        {
            try
            {
                Structure organForPrv = StructureSet.GetStructure(organName);
                Structure prv = StructureSet.GetOrCreateStructure(SupportivePrefix + organName + " " + PrvPostfix, StructureNames.OrgansDicomType);
                prv.SegmentVolume = organForPrv.Margin(margin);
            }
            catch (Exception error)
            {
                Logger.WriteWarning($"Can not create PRV from {organName}: " + error.Message);
            }
        }
    }
}
