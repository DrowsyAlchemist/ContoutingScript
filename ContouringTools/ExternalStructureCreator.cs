using Contouring.Extentions;
using System;
using VMS.TPS.Common.Model.API;

namespace Contouring.Tools
{
    public class ExternalStructureCreator
    {
        private StructureSet StructureSet => Program.StructureSet;

        public ExternalStructureCreator()
        {
        }

        public void Create()
        {
            Logger.WriteInfo("\tExternalStructureCreator: Create");

            try
            {
                Structure ptv = StructureSet.GetStructure(StructureNames.PtvAll);
                Structure body = StructureSet.GetStructure(StructureNames.Body);

                Structure external = StructureSet.GetOrCreateStructure(StructureNames.SupportivePrefix + StructureNames.External);
                external.SegmentVolume = ptv.Margin(Config.ExternalOutMargin);
                external.SegmentVolume = external.Sub(body);

                if (external.IsEmpty)
                {
                    StructureSet.RemoveStructure(external);
                    Logger.WriteInfo("External structure is not needed.");
                    return;
                }
                external.SegmentVolume = external.Margin(Config.ExternalMarginIntoBody);
                external.SetAssignedHU(Config.ExternalHU);
                body.SegmentVolume = body.Or(external);

                if (external.CanConvertToHighResolution())
                    external.ConvertToHighResolution();
            }
            catch (Exception error)
            {
                Logger.WriteError($"Error during creating external structure:\n\t{error}\n");
            }
        }
    }
}
