namespace moddingSuite.Model.Mesh
{
    /// <summary>
    /// One entry in the MultiMaterialMeshes section. Each entry is 4 bytes:
    ///
    ///   bytes [0..1]  ushort  SingleMeshOffset — start index into the SingleMaterialMeshes array
    ///   bytes [2..3]  ushort  SingleMeshCount  — how many consecutive SingleMaterialMesh entries
    ///                                            belong to this multi-material mesh
    ///
    /// Invariant: SingleMeshOffset[n] + SingleMeshCount[n] == SingleMeshOffset[n+1]
    /// </summary>
    public class MultiMaterialMesh
    {
        /// <summary>Start index into the SingleMaterialMeshes array for this group.</summary>
        public ushort SingleMeshOffset { get; set; }

        /// <summary>Number of consecutive SingleMaterialMesh entries in this group.</summary>
        public ushort SingleMeshCount { get; set; }
    }
}
