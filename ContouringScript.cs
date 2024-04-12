using System;
using System.Reflection;
using VMS.TPS.Common.Model.API;

[assembly: AssemblyVersion("3.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.1")]
[assembly: AssemblyInformationalVersion("1.0")]

[assembly: ESAPIScript(IsWriteable = true)]

namespace ContouringScript
{
    public class Program
    {
        public static Application Application { get; private set; }
        public static Patient Patient { get; private set; }
        public static StructureSet StructureSet { get; private set; }

        [STAThread]
        public static void Main()
        {
            try
            {
                using (Application app = Application.CreateApplication())
                {
                    Application = app;
                    Patient = OpenPatientById(app);
                    StructureSet = FindStructureSet(Patient);
                    Execute();
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }
            Console.WriteLine("The program has completed.\nPress any key...");
            Console.ReadKey(true);
        }

        public static void Execute()
        {
            try
            {
                Conture();
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
            Application.ClosePatient();
            Application.Dispose();
        }

        private static void Conture()
        {
            if (Patient.CanModifyData() == false)
                throw new Exception("The program can not modify data.");

            Patient.BeginModifications();

            Logger.WriteInfo("\tCleaner:");
            var cleaner = new Cleaner();
            cleaner.CropStructuresByBody();

            Logger.WriteInfo("\tTargetStructuresCreator:");
            var targetStructuresCreator = new TargetStructuresCreator();
            targetStructuresCreator.Create();
            targetStructuresCreator.CreatePtvOptMinus();//////

            Logger.WriteInfo("\tRingCreator:");
            var ringCreator = new RingCreator();
            ringCreator.Create();

            Logger.WriteInfo("\tExternalStructureCreator:");
            new ExternalStructureCreator().Create();

            Logger.WriteInfo("\tCroppedOrgansCreator:");
            var croppedOrgansCreator = new CroppedOrgansCreator();
            croppedOrgansCreator.Create();
            croppedOrgansCreator.CreateBodyMinusPtv(ringCreator.OuterMarginInMM);//////

            Logger.WriteInfo("\tCleaner:");
            cleaner.RemoveUnnecessaryEmptyStructures();
            Application.SaveModifications();
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

        private static StructureSet FindStructureSet(Patient patient)
        {
            foreach (var structureSet in patient.StructureSets)
                if (structureSet.IsValid())
                    return structureSet;

            throw new Exception("Valid StructureSet is not found.");
        }
    }
}
