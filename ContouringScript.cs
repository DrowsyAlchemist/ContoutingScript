using System;
using System.Reflection;
using VMS.TPS.Common.Model.API;
using Contouring.Tools;
using Contouring.Extentions;

[assembly: AssemblyVersion("3.0.3.0")]
[assembly: AssemblyFileVersion("1.0.0.1")]
[assembly: AssemblyInformationalVersion("1.0")]

[assembly: ESAPIScript(IsWriteable = true)]

namespace Contouring
{
    public class Program
    {
        private static Application _application;
        public static Patient Patient { get; private set; }
        public static StructureSet StructureSet { get; private set; }

        [STAThread]
        public static void Main()
        {
            try
            {
                ConfigParser.SetConfig(typeof(StructureNames));
                ConfigParser.SetConfig(typeof(Config));

                using (Application application = Application.CreateApplication())
                {
                    _application = application;
                    Patient = OpenPatientById(application);
                    InitializePatient(Patient);
                    StructureSet = FindValidStructureSet(Patient);
                    Conture();
                    application.SaveModifications();
                }
            }
            catch (Exception e)
            {
                Logger.WriteError(e.ToString());
            }
            CloseSession(_application);
            Console.WriteLine("The program has completed.\nPress any key...");
            Console.ReadKey(true);
        }

        private static void Conture()
        {
            var croppersFactory = new CroppersFactory();

            var cleaner = new Cleaner(croppersFactory);
            cleaner.CropStructures();

            var targetStructuresCreator = new TargetStructuresCreator(croppersFactory);
            targetStructuresCreator.Create();

            if (GetPermition("Do you need PtvOptMinus?"))
                targetStructuresCreator.CreatePtvOptMinus();

            var ringCreator = new RingCreator(croppersFactory);
            ringCreator.Create();

            var externalStructureCreator = new ExternalStructureCreator();
            externalStructureCreator.Create();

            var croppedOrgansCreator = new CroppedOrgansCreator(croppersFactory);
            croppedOrgansCreator.Create();
            croppedOrgansCreator.CropShoulder();

            if (GetPermition("Do you need BodyMinusPtv?"))
                croppedOrgansCreator.CreateBodyMinusPtv(ringCreator.OuterMarginInMM);

            var prvCreator = new PrvCreator(targetStructuresCreator.MarginFromCtv);
            prvCreator.Create();

            cleaner.RemoveUnnecessaryEmptyStructures();
            croppersFactory.RemoveCroppersStructures();
        }

        private static Patient OpenPatientById(Application application)
        {
            Patient patient = null;

            while (patient == null)
            {
                Console.Write("Patient ID: ");
                string patientId = Console.ReadLine();
                patient = application.OpenPatientById(patientId);

                if (patient == null)
                    Logger.WriteError("Patient is not found.\n");
            }
            return patient;
        }

        private static void InitializePatient(Patient patient)
        {
            if (patient.CanModifyData() == false)
                throw new Exception("The program can not modify data.");

            patient.BeginModifications();
        }

        private static StructureSet FindValidStructureSet(Patient patient)
        {
            foreach (var structureSet in patient.StructureSets)
                if (structureSet.IsValid())
                    return structureSet;

            throw new Exception("Valid StructureSet is not found.");
        }

        private static bool GetPermition(string text)
        {
            while (true)
            {
                Console.WriteLine(text + " (Press Y to accept or N to refuse)");
                var key = Console.ReadKey();
                Console.WriteLine();

                switch (key.Key)
                {
                    case ConsoleKey.Y:
                        return true;
                    case ConsoleKey.N:
                        return false;
                    default:
                        Logger.WriteError("Incorrect input.");
                        break;
                }
            }
        }

        private static void CloseSession(Application application)
        {
            if (application == null)
                return;

            try { application.ClosePatient(); }
            catch { }
            finally { application.Dispose(); }
        }

        //private static void SetConfig()
        //{
        //    string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        //    string[] files = Directory.GetFiles(path);
        //    string cfgFile = null;

        //    foreach (var fileName in files)
        //    {
        //        if (fileName.Contains(".cfg"))
        //            cfgFile = fileName;
        //    }

        //    if (cfgFile == null)
        //    {
        //            List<string> lines = null;


        //        using (FileStream fileStream = File.Create($"{path}/Config.cfg"))
        //        {




        //        }
        //    }

        //    if (cfgFile != null)
        //    {
        //        string[] lines = File.ReadAllLines(cfgFile);
        //        FieldInfo[] configFields = typeof(Config).GetFields();

        //        foreach (var field in configFields)
        //        {
        //            foreach (var line in lines)
        //                if (field.Name.Equals(line))
        //                    field.SetValue(typeof(Config), line.Split('=')[1]);
        //        }
        //    }
        //}
    }
}
