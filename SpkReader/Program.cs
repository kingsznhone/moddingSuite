namespace SpkReader
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var spkFile = @"D:\RD\pc\mesh\pack\gfxdescriptor\mesh_all.spk";

            var fileData = File.ReadAllBytes(spkFile);
            var reader = new moddingSuite.BL.Mesh.MeshReader();
            var spk = reader.Read(fileData, @"D:\RD");

            Console.ReadLine();
        }
    }
}
