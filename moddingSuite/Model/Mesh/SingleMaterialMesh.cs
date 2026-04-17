namespace moddingSuite.Model.Mesh
{
    /// <summary>
    /// One entry in the SingleMaterialMeshes section. Each entry is 12 bytes:
    ///
    ///   bytes [ 0.. 1]  ushort  MultiMaterialMeshIndex — index into MultiMaterialMeshes array
    ///                                                     identifies which multi-material group owns this draw call
    ///   bytes [ 2.. 3]  ushort  UnknownIndex           — purpose TBD; pointer into an unknown table
    ///   bytes [ 4.. 5]  ushort  IndexBufferIndex        — index into Index1DBufferHeaders
    ///                                                     selects the triangle index slice for this draw call
    ///   bytes [ 6.. 7]  ushort  VertexBufferIndex       — index into Vertex1DBufferHeaders
    ///                                                     selects the vertex buffer for this draw call
    ///   bytes [ 8..11]  uint32  Magic                  — always 0xCDCDFFFF (FF FF CD CD); fixed sentinel
    ///
    /// Hierarchy:
    ///   MeshContentFile.MultiMaterialMeshIndex
    ///     → MultiMaterialMesh.SingleMeshOffset / .SingleMeshCount
    ///       → SingleMaterialMesh[]   (one entry per GPU draw call / material slot)
    ///         → IndexBufferIndex  → Index1DBufferHeaders[i]  → triangle indices in Index1DBufferStreams
    ///         → VertexBufferIndex → Vertex1DBufferHeaders[i] → vertex data   in Vertex1DBufferStreams
    /// </summary>
    public class SingleMaterialMesh
    {
        /// <summary>Fixed sentinel word at the end of every entry (0xCDCDFFFF = FF FF CD CD).</summary>
        public const uint Magic = 0xCDCDFFFF;

        /// <summary>
        /// Index into MultiMaterialMeshes — identifies which multi-material group owns this draw call.
        /// </summary>
        public ushort MultiMaterialMeshIndex { get; set; }

        /// <summary>
        /// Purpose TBD — pointer into an as-yet-unidentified table.
        /// </summary>
        public ushort UnknownIndex { get; set; }

        /// <summary>
        /// Index into Index1DBufferHeaders — selects the triangle index buffer slice for this draw call.
        /// </summary>
        public ushort IndexBufferIndex { get; set; }

        /// <summary>
        /// Index into Vertex1DBufferHeaders — selects the vertex buffer slice for this draw call.
        /// </summary>
        public ushort VertexBufferIndex { get; set; }
    }
}
