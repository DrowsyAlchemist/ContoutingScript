using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using VMS.TPS.Common.Model.API;

[assembly: AssemblyVersion("2.1.1.5")]
[assembly: AssemblyFileVersion("1.0.0.1")]
[assembly: AssemblyInformationalVersion("1.0")]

[assembly: ESAPIScript(IsWriteable = true)]

namespace ContouringScript
{
    class Program
    {
        private static Application _application;
        private static StructureSet _structureSet;

        [STAThread]
        static void Main()
        {
            try
            {
                using (Application app = Application.CreateApplication())
                {
                    Execute(app);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }
            Console.WriteLine("The program has completed.\nPress any key...");
            Console.ReadKey(true);
        }

        static void Execute(Application application)
        {
            Logger.IsDebugMode = true;
            _application = application;
            Patient patient = OpenPatientById();

            try
            {
                ModifyPatient(patient);
            }
            catch (Exception error)
            {
                Logger.WriteError(error.ToString());
            }
            try 
            {
                StructuresCropper.RemoveMarginStructures();
            }
            catch { }
            application.ClosePatient();
            application.Dispose();
        }

        private static void ModifyPatient(Patient patient)
        {
            if (patient.CanModifyData() == false)
                throw new Exception("The program can not modify data.");

            patient.BeginModifications();
            _structureSet = FindStructureSet(patient);

            Logger.WriteInfo("\tCleaner:");
            var cleaner = new Cleaner();
            cleaner.CropStructuresByBody();

            Logger.WriteInfo("\tTargetStructuresCreator:");
            new TargetStructuresCreator().Create();

            Logger.WriteInfo("\tRingCreator:");
            var ringCreator = new RingCreator();
            ringCreator.Create();
            uint ringOuterMargin = ringCreator.OuterMarginInMM;

            Logger.WriteInfo("\tExternalStructureCreator:");
            new ExternalStructureCreator().Create();

            Logger.WriteInfo("\tCroppedOrgansCreator:");
            var CroppedOrgansCreator = new CroppedOrgansCreator();
            CroppedOrgansCreator.Create();
            //CroppedOrgansCreator.CropBodyByPTV(ringOuterMargin);

            Logger.WriteInfo("\tCleaner:");
            cleaner.RemoveUnnecessaryEmptyStructures();
            _application.SaveModifications();
        }

        private static Patient OpenPatientById()
        {
            Patient patient = null;

            while (patient == null)
            {
                Console.Write("Patient ID: ");
                string patientId = Console.ReadLine();
                patient = _application.OpenPatientById(patientId);

                if (patient == null)
                    Logger.WriteError("Patient is not found.\n");
            }
            return patient;
        }

        private static StructureSet FindStructureSet(Patient patient)
        {
            foreach (var structureSet in patient.StructureSets)
                if (structureSet.IsValid())
                    return structureSet;

            throw new Exception("Valid StructureSet is not found.");
        }

        private class RingCreator
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

        private class ExternalStructureCreator
        {
            private const double ExternalMargin = 2;
            private const double ExternalMarginIntoBody = 1;
            private const double AssignedHU = -600;

            public void Create()
            {
                try
                {
                    Structure external = _structureSet.GetOrCreateStructure("xExternal");
                    Structure ptv = _structureSet.GetStructure("PTV_all");
                    Structure body = _structureSet.GetStructure("BODY"); ;

                    external.SegmentVolume = ptv.Margin(ExternalMargin);
                    external.SegmentVolume = external.Sub(body);

                    if (external.IsEmpty)
                    {
                        _structureSet.RemoveStructure(external);
                        Logger.WriteWarning("External structure is not needed.");
                        return;
                    }
                    external.SegmentVolume = external.Margin(ExternalMarginIntoBody);
                    external.SetAssignedHU(AssignedHU);
                    body.SegmentVolume = body.Or(external);

                    if (external.CanConvertToHighResolution())
                        external.ConvertToHighResolution();

                    _application.SaveModifications();
                }
                catch (Exception error)
                {
                    Logger.WriteError($"Error during creating external structure:\n\t{error}\n");
                }
            }
        }

        private class TargetStructuresCreator
        {
            private const uint MarginUpperBoundInMM = 50;
            private const double PtvCroppedVolumeTreshhold = 2;

            public void Create()
            {
                try
                {
                    Structure[] ctvs = CreateCtvs();
                    CreatePtvsFromCtvs(ctvs);
                    _application.SaveModifications();
                }
                catch (Exception error)
                {
                    Logger.WriteError($"Error during creating ptv:\n\t{error}\n");
                }
                if (PtvAllIsFilled())
                {
                    CreatePtvOptFrom("PTV_all");
                    CreatePtvOptFrom("PTV_t");
                }
                else
                {
                    throw new Exception("PTV_all is not filled.");
                }
            }

            private Structure[] CreateCtvs()
            {
                Structure[] ctvs = GetFilledCTVs();

                if (CtvIsHighResolution(ctvs))
                    throw new Exception("Error: CTV is high resolution.");

                Structure ctvAll = _structureSet.GetOrCreateStructure("CTV_all", dicomType: "CTV");
                MergeStructures(ctvAll, ctvs);
                ctvs.Append(ctvAll);
                Structure[] ctvLymphNodes = GetFilledCtvLymphNodes(ctvs);

                if (ctvLymphNodes.Any())
                {
                    Structure ctvLn = _structureSet.GetOrCreateStructure("CTV_l/n", dicomType: "CTV");
                    MergeStructures(ctvLn, ctvLymphNodes);
                    ctvs.Append(ctvLn);
                }
                return ctvs;
            }

            private Structure[] GetFilledCTVs()
            {
                try
                {
                    return _structureSet.Structures
                        .Where(s => s.Id.ToLower().StartsWith("ctv"))
                        .Where(s => s.IsEmpty == false)
                        .ToArray();
                }
                catch (ArgumentNullException)
                {
                    throw new Exception("Can not find filled ctv");
                }
            }

            private Structure[] GetFilledCtvLymphNodes(Structure[] ctvs)
            {
                try
                {
                    return ctvs
                        .Where(ctv => ctv.Id.ToLower().Contains("ax")
                        || ctv.Id.ToLower().Contains("l/n")
                        || ctv.Id.ToLower().Contains("parastern")
                        || ctv.Id.ToLower().Contains("subclav")
                        || ctv.Id.ToLower() == "ctv_n")
                        .Where(ctv => ctv.IsEmpty == false)
                        .ToArray();
                }
                catch (ArgumentNullException)
                {
                    Logger.WriteWarning("There is not lymph nodes structures.");
                    return new Structure[0];
                }
            }

            private void MergeStructures(Structure combinedStructure, Structure[] structures)
            {
                foreach (var structure in structures)
                    combinedStructure.SegmentVolume = combinedStructure.Or(structure);
            }

            private bool CtvIsHighResolution(Structure[] ctvs)
            {
                foreach (Structure ctv in ctvs)
                    if (ctv.IsHighResolution)
                        return true;
                return false;
            }

            private void CreatePtvsFromCtvs(Structure[] ctvs)
            {
                List<Structure> ptvs = new List<Structure>();
                uint marginFromCtvInMM = GetMargin();

                foreach (Structure ctv in ctvs)
                {
                    try
                    {
                        ptvs.Add(CreatePtvFromCtv(ctv, marginFromCtvInMM));
                    }
                    catch (Exception error)
                    {
                        Logger.WriteError($"Can not create ptv from \"{ctv.Id}\":\n\t{error}\n");
                    }
                }
                Structure ptvAll = _structureSet.GetOrCreateStructure("PTV_all", dicomType: "PTV");
                MergeStructures(ptvAll, ptvs.ToArray());

                if (_structureSet.GetStructure("PTV_all").IsEmpty)
                    throw new Exception("PTV_all is empty.");
            }

            private Structure CreatePtvFromCtv(Structure ctv, uint marginFromCtvInMM)
            {
                string ptvName = "PTV" + ctv.Id.Substring(3);
                Structure ptv = _structureSet.GetOrCreateStructure(ptvName, dicomType: "PTV");
                ptv.SegmentVolume = ctv.Margin(marginFromCtvInMM);
                return ptv;
            }

            private uint GetMargin()
            {
                uint marginInMM = 0;
                bool isCorrectMargin = false;

                while (isCorrectMargin == false)
                {
                    Console.Write("Ptv margin in mm: ");
                    isCorrectMargin = uint.TryParse(Console.ReadLine(), out marginInMM);

                    if (marginInMM <= 0)
                    {
                        Logger.WriteError("Margin should be positive integer.");
                        isCorrectMargin = false;
                    }
                    if (marginInMM > MarginUpperBoundInMM)
                    {
                        Logger.WriteError("Margin should be less than 50 mm.");
                        isCorrectMargin = false;
                    }
                }
                return marginInMM;
            }

            private bool PtvAllIsFilled()
            {
                try
                {
                    Structure ptvAll = _structureSet.GetStructure("PTV_all");
                    return (ptvAll.IsEmpty == false);
                }
                catch
                {
                    return false;
                }
            }

            private void CreatePtvOptFrom(string ptvName)
            {
                try
                {
                    Structure body = _structureSet.GetStructure("body");
                    var cropperByBody = new StructuresCropper(body);
                    Structure ptv = _structureSet.GetStructure(ptvName);
                    string ptvOptName;

                    if (ptvName.ToLower().Contains("all"))
                        ptvOptName = "PTV_Opt";
                    else
                        ptvOptName = ptvName + "_Opt";

                    Structure ptvOpt = _structureSet.GetOrCreateStructure(ptvOptName, dicomType: "PTV");
                    ptvOpt.SegmentVolume = cropperByBody.Crop(ptv, 1, removePartInside: false);

                    if (Math.Abs(ptvOpt.Volume - ptv.Volume) < PtvCroppedVolumeTreshhold)
                        _structureSet.RemoveStructure(ptvOpt);
                }
                catch (Exception error)
                {
                    Logger.WriteWarning($"Can not create ptvOpt from \"{ptvName}\":\n{error}\n");
                }
            }
        }

        private class CroppedOrgansCreator
        {
            private const string CropPostfix = "-PTV";
            private const string CropPrefix = "x";
            private const uint CropMargin = 3;
            private const double CropVolumeTrashhold = 1.5;
            private readonly StructuresCropper _cropperByPTV = null;

            public CroppedOrgansCreator()
            {
                Structure ptv = GetPtv();
                _cropperByPTV = new StructuresCropper(ptv);
            }

            public void Create()
            {
                Structure[] organs = GetOrgans();

                for (int i = 0; i < organs.Length; i++)
                {
                    try
                    {
                        CreateCroppedOrganFrom(organs[i]);
                    }
                    catch (Exception error)
                    {
                        Logger.WriteError($"Can not crop {organs[i].Id}:\n{error}\n");
                    }
                }
                _application.SaveModifications();
            }

            public void CropBodyByPTV(uint marginInMM)
            {
                try
                {
                    Structure body = _structureSet.GetStructure("BODY");
                    Structure bodyMinusPTV = _structureSet.GetOrCreateStructure("xb0dy-PTV");
                    bodyMinusPTV.SegmentVolume = _cropperByPTV.Crop(body, marginInMM);
                    _application.SaveModifications();
                }
                catch (Exception)
                {
                    Logger.WriteError("Can not create \"body-ptv\"");
                }
            }

            private Structure GetPtv()
            {
                Structure ptv = _structureSet.GetStructure("PTV_all");

                if (ptv.IsEmpty)
                    throw new Exception("PTV_all is empty.");

                return ptv;
            }

            private Structure[] GetOrgans()
            {
                return _structureSet.Structures
                    .Where(s => s.DicomType == "ORGAN")
                    .Where(s => s.Id.StartsWith(CropPrefix) == false)
                    .Where(s => s.Id != "Bones")
                    .Where(s => s.Id.StartsWith("CTV") == false)
                    .Where(s => s.Id.StartsWith("PTV") == false)
                    .ToArray();
            }

            private void CreateCroppedOrganFrom(Structure organ)
            {
                string cropedOrganName = GetCroppedOrganName(organ.Id);
                Structure croppedOrgan = _structureSet.GetOrCreateStructure(cropedOrganName);

                if (organ.IsHighResolution)
                    throw new Exception($"{organ.Id} is high resolution.");

                croppedOrgan.SegmentVolume = _cropperByPTV.Crop(organ, CropMargin);

                if (IsInvalidCroppedVolume(organ, croppedOrgan))
                    RemoveCroppedOrgan(croppedOrgan);
            }

            private string GetCroppedOrganName(string initialOrganName)
            {
                string croppedOrganName = CropPrefix + initialOrganName + CropPostfix;

                if (croppedOrganName.Length > 16)
                    croppedOrganName = croppedOrganName.Substring(0, 10) + croppedOrganName.Substring(croppedOrganName.Length - 6);

                return croppedOrganName;
            }

            private bool IsInvalidCroppedVolume(Structure initialOrgan, Structure croppedOrgan)
            {
                if (Math.Abs(initialOrgan.Volume - croppedOrgan.Volume) < CropVolumeTrashhold)
                {
                    Logger.WriteWarning($"\"{initialOrgan.Id}\" cropped volume is less than {CropVolumeTrashhold}");
                    return true;
                }
                return false;
            }

            private void RemoveCroppedOrgan(Structure croppedOrgan)
            {
                Logger.WriteWarning($"Structure \"{croppedOrgan.Id}\"({croppedOrgan.Volume}) has been removed.");
                _structureSet.RemoveStructure(croppedOrgan);
            }
        }

        private class StructuresCropper
        {
            private const string MarginStructureName = "Margin structure";
            private static int _instanceCount = 0;
            private readonly Structure _structureByWhichCrop;
            private readonly Structure _marginStructure;

            public StructuresCropper(Structure structureByWhichCrop)
            {
                _instanceCount++;
                _structureByWhichCrop = structureByWhichCrop ?? throw new ArgumentNullException(nameof(structureByWhichCrop));
                _marginStructure = _structureSet.CreateStructure(MarginStructureName + "_" + _instanceCount);
            }

            public static void RemoveMarginStructures()
            {
                var marginStructures = _structureSet.Structures.Where(s => s.Id.ToLower().Contains("margin")).ToArray();
                Logger.WriteWarning($"There are {marginStructures.Length} margin structures.");

                foreach (var marginStructure in marginStructures)
                    RemoveMarginStructure(marginStructure);

                _application.SaveModifications();
            }

            public SegmentVolume Crop(Structure structure, uint marginInMM, bool removePartInside = true)
            {
                double cropMargin = marginInMM * (removePartInside ? 1 : -1);
                _marginStructure.SegmentVolume = _structureByWhichCrop.Margin(cropMargin);
                SegmentVolume croppedVolume;

                if (removePartInside)
                    croppedVolume = structure.Sub(_marginStructure);
                else
                    croppedVolume = structure.And(_marginStructure);

                return croppedVolume;
            }

            private static void RemoveMarginStructure(Structure marginStructure)
            {
                if (_structureSet.CanRemoveStructure(marginStructure))
                {
                    Logger.WriteInfo($"{marginStructure.Id} has been removed.");
                    _structureSet.RemoveStructure(marginStructure);
                }
                else
                {
                    Logger.WriteError($"Can not remove {marginStructure.Id}.");
                }
            }
        }

        private class Cleaner
        {
            private readonly StructuresCropper _cropperByBody = null;

            public Cleaner()
            {
                _cropperByBody = GetCropperByBody();
            }

            public void RemoveUnnecessaryEmptyStructures()
            {
                Structure[] structuresToRemove = GetUnnecessaryEmptyStructures();

                foreach (var structure in structuresToRemove)
                {
                    if (_structureSet.CanRemoveStructure(structure))
                    {
                        Logger.WriteWarning($"Structure \"{structure.Id}\" has been removed.");
                        _structureSet.RemoveStructure(structure);
                    }
                    else
                    {
                        Logger.WriteWarning($"Structure \"{structure.Id}\" can not be removed.");
                    }
                }
            }

            public void CropStructuresByBody()
            {
                var structuresToCrop = _structureSet.Structures
                    .Where(s => s.DicomType == "ORGAN"
                    || s.DicomType == "CTV");

                foreach (var structure in structuresToCrop)
                {
                    if (structure.IsHighResolution)
                        Logger.WriteError($"Can not crop \"{structure.Id}\" by body because it's high resolution.");
                    else
                        CropStructureByBody(structure);
                }
                _application.SaveModifications();
            }

            private void CropStructureByBody(Structure structure)
            {
                try
                {
                    structure.SegmentVolume = _cropperByBody.Crop(structure, marginInMM: 0, removePartInside: false);
                }
                catch (Exception error)
                {
                    Logger.WriteError($"Error during cropping structure \"{structure.Id}\" by body:\n\t{error}\n");
                }
            }

            private Structure[] GetUnnecessaryEmptyStructures()
            {
                try
                {
                    return _structureSet.Structures
                        .Where(s => s.IsEmpty)
                        .Where(s => s.Id.ToLower().Contains("prv") == false)
                        .Where(s => s.Id.ToLower().Contains("shoulder") == false)
                        .ToArray();
                }
                catch (ArgumentNullException)
                {
                    Logger.WriteWarning("There are no unnecessary empty structures.");
                    return new Structure[0];
                }
            }

            private StructuresCropper GetCropperByBody()
            {
                return new StructuresCropper(_structureSet.GetStructure("body"));
            }
        }
    }
}
