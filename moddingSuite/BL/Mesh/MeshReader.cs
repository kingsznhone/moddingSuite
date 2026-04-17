using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using moddingSuite.BL.Ndf;
using moddingSuite.Model.Edata;
using moddingSuite.Model.Mesh;
using System.Runtime.InteropServices;
using moddingSuite.Util;

namespace moddingSuite.BL.Mesh
{
    public class MeshReader
    {
        public const uint MeshMagic = 1213416781; // "MESH"
        public const uint ProxyMagic = 1498960464; // "PRXY"

        public MeshFile Read(byte[] data, string dumpDir = null)
        {
            using (var ms = new MemoryStream(data))
                return Read(ms, dumpDir);
        }

        public MeshFile Read(Stream s, string dumpDir = null)
        {
            long totalBytes = s.Length;
            var file = new MeshFile();

            file.Header = ReadHeader(s);

            file.SubHeader = ReadSubHeader(s);

            file.MultiMaterialMeshFiles = ReadMeshDictionary(s, file);

            file.VertexTypeNames = ReadVertexTypeNames(s, file);

            file.TextureBindings = ReadTextureBindings(s, file);

            file.MultiMaterialMeshes = ReadMultiMaterialMeshes(s, file);

            file.SingleMaterialMeshes = ReadSingleMaterialMeshes(s, file);

            file.Index1DBufferHeaders = ReadIndex1DBufferHeaders(s, file);
            file.IndexBufferSlices = ReadIndexBufferStreams(s, file);

            file.Vertex1DBufferHeaders = ReadVertex1DBufferHeaders(s, file);
            file.VertexBufferSlices = ReadVertexBufferStreams(s, file);

            // ── Unified offset table ─────────────────────────────────────────────────
            var sh = file.SubHeader;
            var sections = new List<(string Name, long Offset, long Size, long Count)>
            {
                // ✅ PARSED — ReadHeader()  →  MeshHeader model
                ("FileHeader",              0,                                   48,                                 -1),

                // ✅ PARSED — ReadSubHeader()  →  MeshSubHeader model (directory of all section offsets)
                ("SubHeader",               file.Header.HeaderOffset,            file.Header.HeaderSize,             -1),

                // ✅ PARSED — ReadMeshDictionary()  →  ObservableCollection<MeshContentFile>
                //            Hierarchical file/dir tree; each leaf carries bounding-box, flags, MMM index
                ("Dictionary",              sh.Dictionary.Offset,                sh.Dictionary.Size,                 sh.Dictionary.Count),

                // ✅ PARSED — ReadVertexTypeNames()  →  List<string>
                //            4-byte header (slotSize=256) + Count × 256-byte null-padded ASCII strings
                //            index = S2 field in Vertex1DBufferHeader
                ("VertexTypeNames",         sh.VertexTypeNames.Offset,           sh.VertexTypeNames.Size,            sh.VertexTypeNames.Count),

                // ✅ PARSED — ReadTextureBindings()  →  NdfBinary
                //            NDF binary blob that binds texture paths to mesh materials
                ("MeshMaterial_NDF",        sh.MeshMaterial.Offset,              sh.MeshMaterial.Size,               sh.MeshMaterial.Count),

                // ❌ NOT PARSED — per-LOD sub-mesh descriptors (material slot, index/vertex range)
                ("KeyedMeshSubPart",        sh.KeyedMeshSubPart.Offset,          sh.KeyedMeshSubPart.Size,           sh.KeyedMeshSubPart.Count),

                // ❌ NOT PARSED — float vectors associated with keyed sub-mesh parts (likely bone/LOD pivots)
                ("KeyedMeshSubPartVectors", sh.KeyedMeshSubPartVectors.Offset,   sh.KeyedMeshSubPartVectors.Size,    sh.KeyedMeshSubPartVectors.Count),

                // ✅ PARSED — ReadMultiMaterialMeshes()  →  List<MultiMaterialMesh>
                //            each 4-byte entry: ushort SingleMeshOffset + ushort SingleMeshCount (into SingleMaterialMeshes)
                ("MultiMaterialMeshes",     sh.MultiMaterialMeshes.Offset,       sh.MultiMaterialMeshes.Size,        sh.MultiMaterialMeshes.Count),

                // ✅ PARSED — ReadSingleMaterialMeshes()  →  List<SingleMaterialMesh>
                //            12-byte entry: VertexBufferIndex + IndexBufferIndex + S2 + S3 + magic(FFFF CDCD)
                ("SingleMaterialMeshes",    sh.SingleMaterialMeshes.Offset,      sh.SingleMaterialMeshes.Size,       sh.SingleMaterialMeshes.Count),

                // ✅ PARSED — ReadIndex1DBufferHeaders()  →  List<Index1DBufferHeader>
                //            S2 always 1; S0 meaning TBD (< 65536, non-integer ratio to Length)
                ("Index1DBufferHeaders",    sh.Index1DBufferHeaders.Offset,      sh.Index1DBufferHeaders.Size,       sh.Index1DBufferHeaders.Count),

                // ✅ PARSED — ReadIndexBufferStreams()  →  List<ushort[]>  (IndexStride detected: 2 or 4)
                //            slice[i] = triangle indices for Index1DBufferHeaders[i]
                ("Index1DBufferStreams",    sh.Index1DBufferStreams.Offset,       sh.Index1DBufferStreams.Size,       -1),

                // ✅ PARSED — ReadVertex1DBufferHeaders()  →  List<Vertex1DBufferHeader>
                //            S2 = vertex format enum (0..3), maps into VertexTypeNames string table
                ("Vertex1DBufferHeaders",   sh.Vertex1DBufferHeaders.Offset,     sh.Vertex1DBufferHeaders.Size,      sh.Vertex1DBufferHeaders.Count),

                // ✅ PARSED — ReadVertexBufferStreams()  →  List<byte[]>
                //            slice[i] = raw vertex bytes for Vertex1DBufferHeaders[i]; stride = VertexFormats[S2].Stride
                ("Vertex1DBufferStreams",   sh.Vertex1DBufferStreams.Offset,      sh.Vertex1DBufferStreams.Size,      -1),
            };

            // Sort by byte offset so the table and file indices reflect actual order
            sections.Sort((a, b) => a.Offset.CompareTo(b.Offset));

            Console.WriteLine();
            Console.WriteLine($"[MeshReader] === Unified offset table  (total file: {totalBytes} bytes) ===");
            Console.WriteLine($"[MeshReader]   {"#",-3}  {"Name",-26}  {"Offset",10}  {"End",10}  {"Size",12}  {"Count",8}");
            Console.WriteLine($"[MeshReader]   {new string('-', 80)}");
            for (int i = 0; i < sections.Count; i++)
            {
                var (name, offset, size, count) = sections[i];
                string countStr = count >= 0 ? count.ToString() : "-";
                Console.WriteLine($"[MeshReader]   {i,-3}  {name,-26}  {offset,10}  {offset + size,10}  {size,12}  {countStr,8}");
            }

            // Report gaps between consecutive sections
            Console.WriteLine();
            Console.WriteLine($"[MeshReader] === Gap analysis ===");
            for (int i = 0; i < sections.Count - 1; i++)
            {
                long gapStart = sections[i].Offset + sections[i].Size;
                long gapEnd   = sections[i + 1].Offset;
                if (gapEnd > gapStart)
                    Console.WriteLine($"[MeshReader]   GAP between [{sections[i].Name}] and [{sections[i + 1].Name}]: {gapEnd - gapStart} bytes at offset {gapStart}");
            }
            long tail = totalBytes - (sections[^1].Offset + sections[^1].Size);
            if (tail > 0)
                Console.WriteLine($"[MeshReader]   TAIL after [{sections[^1].Name}]: {tail} bytes");

            // ── Dump sections to bin files ───────────────────────────────────────────
            if (dumpDir != null)
            {
                Directory.CreateDirectory(dumpDir);
                Console.WriteLine();
                Console.WriteLine($"[MeshReader] === Dumping sections to: {dumpDir} ===");
                var buf = new byte[81920];
                for (int i = 0; i < sections.Count; i++)
                {
                    var (name, offset, size, _) = sections[i];
                    if (size <= 0) continue;

                    string path = Path.Combine(dumpDir, $"{i:D2}_{name}.bin");
                    s.Seek(offset, SeekOrigin.Begin);

                    using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
                    long remaining = size;
                    while (remaining > 0)
                    {
                        int read = s.Read(buf, 0, (int)Math.Min(buf.Length, remaining));
                        if (read == 0) break;
                        fs.Write(buf, 0, read);
                        remaining -= read;
                    }
                    Console.WriteLine($"[MeshReader]   [{i:D2}] {name,-26}  -> {Path.GetFileName(path)}  ({size} bytes)");
                }

                // ── Dump individual buffer slices ────────────────────────────────────
                DumpBufferSlices(dumpDir, "Index1DBufferStreams",  file.IndexBufferSlices);
                DumpBufferSlices(dumpDir, "Vertex1DBufferStreams", file.VertexBufferSlices);

                // ── Dump mesh hierarchy as JSON ───────────────────────────────────────
                DumpMeshHierarchyJson(dumpDir, file);
            }

            return file;
        }

        /// <summary>
        /// Dumps MultiMaterialMeshFiles, MultiMaterialMeshes, and SingleMaterialMeshes as JSON.
        ///
        /// Three files are written to <paramref name="dumpDir"/>:
        ///   MultiMaterialMeshFiles.json   — MeshContentFile array (name, path, bounding box, flags, MMM index)
        ///   MultiMaterialMeshes.json      — MultiMaterialMesh array (SingleMeshOffset, SingleMeshCount)
        ///   SingleMaterialMeshes.json     — SingleMaterialMesh array (all four ushort fields)
        ///   mesh_hierarchy.json           — combined: each MultiMaterialMesh with its MeshContentFile
        ///                                   and the SingleMaterialMesh children inlined
        /// </summary>
        private static void DumpMeshHierarchyJson(string dumpDir, MeshFile file)
        {
            var opts = new JsonSerializerOptions
            {
                WriteIndented     = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            };

            // ── raw tables ───────────────────────────────────────────────────────────
            WriteJson(dumpDir, "MultiMaterialMeshFiles.json",
                file.MultiMaterialMeshFiles?.Select((f, i) => new
                {
                    Index    = i,
                    f.Name,
                    f.Path,
                    f.Flags,
                    f.MultiMaterialMeshIndex,
                    f.HierarchicalAseModelSkeletonIndex,
                    MinBB = new { f.MinBoundingBox.X, f.MinBoundingBox.Y, f.MinBoundingBox.Z },
                    MaxBB = new { f.MaxBoundingBox.X, f.MaxBoundingBox.Y, f.MaxBoundingBox.Z },
                }),
                opts);

            WriteJson(dumpDir, "MultiMaterialMeshes.json",
                file.MultiMaterialMeshes?.Select((m, i) => new
                {
                    Index            = i,
                    m.SingleMeshOffset,
                    m.SingleMeshCount,
                }),
                opts);

            WriteJson(dumpDir, "SingleMaterialMeshes.json",
                file.SingleMaterialMeshes?.Select((m, i) => new
                {
                    Index                  = i,
                    m.MultiMaterialMeshIndex,
                    m.UnknownIndex,
                    m.IndexBufferIndex,
                    m.VertexBufferIndex,
                }),
                opts);

            // ── combined hierarchy ───────────────────────────────────────────────────
            var smm  = file.SingleMaterialMeshes;
            var mmm  = file.MultiMaterialMeshes;
            var mmf  = file.MultiMaterialMeshFiles;

            var combined = mmm?.Select((m, i) => new
            {
                Index            = i,
                MeshFile         = (mmf != null && i < mmf.Count) ? new
                {
                    mmf[i].Name,
                    mmf[i].Path,
                    mmf[i].Flags,
                    mmf[i].HierarchicalAseModelSkeletonIndex,
                    MinBB = new { mmf[i].MinBoundingBox.X, mmf[i].MinBoundingBox.Y, mmf[i].MinBoundingBox.Z },
                    MaxBB = new { mmf[i].MaxBoundingBox.X, mmf[i].MaxBoundingBox.Y, mmf[i].MaxBoundingBox.Z },
                } : null,
                m.SingleMeshOffset,
                m.SingleMeshCount,
                DrawCalls = smm?
                    .Skip(m.SingleMeshOffset)
                    .Take(m.SingleMeshCount)
                    .Select((s, j) => new
                    {
                        LocalIndex        = j,
                        GlobalIndex       = m.SingleMeshOffset + j,
                        s.UnknownIndex,
                        s.IndexBufferIndex,
                        s.VertexBufferIndex,
                    })
                    .ToList(),
            });

            WriteJson(dumpDir, "mesh_hierarchy.json", combined, opts);

            Console.WriteLine();
            Console.WriteLine($"[MeshReader] === JSON hierarchy dumped to: {dumpDir} ===");
            Console.WriteLine($"[MeshReader]   MultiMaterialMeshFiles.json  ({mmf?.Count ?? 0} entries)");
            Console.WriteLine($"[MeshReader]   MultiMaterialMeshes.json     ({mmm?.Count ?? 0} entries)");
            Console.WriteLine($"[MeshReader]   SingleMaterialMeshes.json    ({smm?.Count ?? 0} entries)");
            Console.WriteLine($"[MeshReader]   mesh_hierarchy.json          (combined)");
        }

        private static void WriteJson<T>(string dir, string fileName, T value, JsonSerializerOptions opts)
        {
            string path = Path.Combine(dir, fileName);
            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            JsonSerializer.Serialize(fs, value, opts);
        }

        // Prints offset/size/count for a section entry
        private static void PrintSection(string name, MeshHeaderEntry e)
        {
            Console.WriteLine($"[MeshReader]   {name,-26}  offset={e.Offset,10}  size={e.Size,12}");
        }

        private static void PrintSection(string name, MeshHeaderEntryWithCount e)
        {
            Console.WriteLine($"[MeshReader]   {name,-26}  offset={e.Offset,10}  size={e.Size,12}  count={e.Count,6}");
        }

        /// <summary>
        /// Dumps each element of <paramref name="slices"/> to a file named by its zero-based index
        /// (no extension) inside <c><paramref name="dumpDir"/>/<paramref name="folderName"/>/</c>.
        /// </summary>
        private static void DumpBufferSlices<T>(
            string dumpDir,
            string folderName,
            List<T[]> slices) where T : struct
        {
            if (slices == null || slices.Count == 0) return;

            string dir = Path.Combine(dumpDir, folderName);
            Directory.CreateDirectory(dir);

            Console.WriteLine();
            Console.WriteLine($"[MeshReader] === Dumping {folderName} ({slices.Count} slices) → {dir} ===");
            Console.WriteLine($"[MeshReader]   {"#",-5}  {"Bytes",10}  {"Header (first 32 bytes)"}");
            Console.WriteLine($"[MeshReader]   {new string('-', 80)}");

            int elementSize = System.Runtime.InteropServices.Marshal.SizeOf<T>();
            for (int i = 0; i < slices.Count; i++)
            {
                string path = Path.Combine(dir, $"{i}.bin");
                using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
                var slice = slices[i];
                var bytes = new byte[slice.Length * elementSize];
                System.Buffer.BlockCopy(slice, 0, bytes, 0, bytes.Length);
                fs.Write(bytes, 0, bytes.Length);

                int previewLen = Math.Min(32, bytes.Length);
                string hex = BitConverter.ToString(bytes, 0, previewLen).Replace("-", " ");
                //Console.WriteLine($"[MeshReader]   {i,-5}  {bytes.Length,10}  {hex}");
            }
        }

        /// <summary>
        /// Parses the VertexTypeNames section.
        ///
        /// Layout:
        ///   uint32  slotSize   — always 256 (0x00000100); the fixed byte width of each name slot
        ///   char[256] × Count  — null-terminated ASCII strings, zero-padded to slotSize bytes
        ///
        /// The index of each name corresponds to the S2 field in Vertex1DBufferHeader,
        /// identifying the vertex format / attribute layout for that buffer.
        /// </summary>
        protected List<string> ReadVertexTypeNames(Stream s, MeshFile file)
        {
            var section = file.SubHeader.VertexTypeNames;
            s.Seek(section.Offset, SeekOrigin.Begin);

            var header = new byte[4];
            s.Read(header, 0, 4);
            uint slotSize = BitConverter.ToUInt32(header, 0); // expected: 256

            var names   = new List<string>((int)section.Count);
            var formats = new List<Model.Mesh.VertexFormat>((int)section.Count);
            var slot    = new byte[slotSize];
            for (uint i = 0; i < section.Count; i++)
            {
                s.Read(slot, 0, (int)slotSize);
                int len = Array.IndexOf(slot, (byte)0);
                if (len < 0) len = (int)slotSize;
                string name = Encoding.ASCII.GetString(slot, 0, len);
                names.Add(name);
                formats.Add(VertexFormatParser.Parse(name));
            }

            file.VertexFormats = formats;

            Console.WriteLine();
            Console.WriteLine($"[MeshReader] === VertexTypeNames (slotSize={slotSize}, count={names.Count}) ===");
            for (int i = 0; i < formats.Count; i++)
            {
                var fmt = formats[i];
                Console.WriteLine($"[MeshReader]   [{i}] stride={fmt.Stride,3}B  \"{names[i]}\"");
                foreach (var attr in fmt.Attributes)
                    Console.WriteLine($"[MeshReader]        +{attr.ByteSize,3}B  {attr}");
            }

            return names;
        }

        private Model.Ndfbin.NdfBinary ReadTextureBindings(Stream s, MeshFile file)
        {
            var buffer = new byte[file.SubHeader.MeshMaterial.Size];

            s.Seek(file.SubHeader.MeshMaterial.Offset, SeekOrigin.Begin);
            s.Read(buffer, 0, buffer.Length);

            var ndfReader = new NdfbinReader();
            return ndfReader.Read(buffer);
        }

        /// <summary>
        /// Parses the MultiMaterialMeshes section. Each entry is 4 bytes:
        ///   bytes [0..1]  ushort  SingleMeshOffset — start index into the SingleMaterialMeshes array
        ///   bytes [2..3]  ushort  SingleMeshCount  — number of consecutive SingleMaterialMesh entries
        ///
        /// Invariant: SingleMeshOffset[n] + SingleMeshCount[n] == SingleMeshOffset[n+1]
        /// </summary>
        protected List<Model.Mesh.MultiMaterialMesh> ReadMultiMaterialMeshes(Stream s, MeshFile file)
        {
            var section = file.SubHeader.MultiMaterialMeshes;
            var entries = new List<Model.Mesh.MultiMaterialMesh>((int)section.Count);

            s.Seek(section.Offset, SeekOrigin.Begin);

            var buf = new byte[2];
            for (uint i = 0; i < section.Count; i++)
            {
                var e = new Model.Mesh.MultiMaterialMesh();
                s.Read(buf, 0, 2); e.SingleMeshOffset = BitConverter.ToUInt16(buf, 0);
                s.Read(buf, 0, 2); e.SingleMeshCount  = BitConverter.ToUInt16(buf, 0);
                entries.Add(e);
            }

            //Console.WriteLine();
            //Console.WriteLine($"[MeshReader] === MultiMaterialMeshes ({entries.Count} entries) ===");
            //Console.WriteLine($"[MeshReader]   {"#",-5}  {"Offset",8}  {"Count",6}  {"End",8}");
            //Console.WriteLine($"[MeshReader]   {new string('-', 40)}");
            //for (int i = 0; i < entries.Count; i++)
            //{
            //    var e = entries[i];
            //    Console.WriteLine($"[MeshReader]   {i,-5}  {e.SingleMeshOffset,8}  {e.SingleMeshCount,6}  {e.SingleMeshOffset + e.SingleMeshCount,8}");
            //}

            return entries;
        }

        /// <summary>
        /// Parses the SingleMaterialMeshes section. Each entry is 12 bytes:
        ///   bytes [0..1]  ushort  VertexBufferIndex — index into Vertex1DBufferHeaders
        ///   bytes [2..3]  ushort  IndexBufferIndex  — index into Index1DBufferHeaders
        ///   bytes [4..5]  ushort  S2               — TBD (candidate: KeyedMeshSubPart index)
        ///   bytes [6..7]  ushort  S3               — TBD (candidate: KeyedMeshSubPartVector index;
        ///                                            S3 − S2 = 28 constant in large-file data)
        ///   bytes [8..11] uint32  Magic            — always 0xCDCDFFFF (FF FF CD CD)
        /// </summary>
        protected List<Model.Mesh.SingleMaterialMesh> ReadSingleMaterialMeshes(Stream s, MeshFile file)
        {
            var section = file.SubHeader.SingleMaterialMeshes;
            var entries = new List<Model.Mesh.SingleMaterialMesh>((int)section.Count);

            s.Seek(section.Offset, SeekOrigin.Begin);

            var buf2 = new byte[2];
            var buf4 = new byte[4];
            for (uint i = 0; i < section.Count; i++)
            {
                var e = new Model.Mesh.SingleMaterialMesh();
                s.Read(buf2, 0, 2); e.MultiMaterialMeshIndex = BitConverter.ToUInt16(buf2, 0);
                s.Read(buf2, 0, 2); e.UnknownIndex          = BitConverter.ToUInt16(buf2, 0);
                s.Read(buf2, 0, 2); e.IndexBufferIndex       = BitConverter.ToUInt16(buf2, 0);
                s.Read(buf2, 0, 2); e.VertexBufferIndex      = BitConverter.ToUInt16(buf2, 0);
                s.Read(buf4, 0, 4); // skip fixed magic 0xCDCDFFFF
                entries.Add(e);
            }

            //Console.WriteLine();
            //Console.WriteLine($"[MeshReader] === SingleMaterialMeshes ({entries.Count} entries) ===");
            //Console.WriteLine($"[MeshReader]   {"#",-5}  {"VtxBuf",8}  {"IdxBuf",8}  {"S2",8}  {"S3",8}  {"S3-S2",6}");
            //Console.WriteLine($"[MeshReader]   {new string('-', 55)}");
            //for (int i = 0; i < entries.Count; i++)
            //{
            //    var e = entries[i];
            //    Console.WriteLine($"[MeshReader]   {i,-5}  {e.VertexBufferIndex,8}  {e.IndexBufferIndex,8}  {e.S2,8}  {e.S3,8}  {e.S3 - e.S2,6}");
            //}

            // ── Field statistics: max value of each ushort field ─────────────────────
            // Helps identify which lookup table each field indexes into.
            // Compare against: Vertex1DBufferHeaders.Count, Index1DBufferHeaders.Count,
            //                  KeyedMeshSubPart.Count, KeyedMeshSubPartVectors.Count, etc.
            ushort maxMmm = 0, maxUnk = 0, maxIdx = 0, maxVtx = 0;
            ushort minMmm = ushort.MaxValue, minUnk = ushort.MaxValue, minIdx = ushort.MaxValue, minVtx = ushort.MaxValue;
            foreach (var e in entries)
            {
                if (e.MultiMaterialMeshIndex > maxMmm) maxMmm = e.MultiMaterialMeshIndex;
                if (e.UnknownIndex           > maxUnk) maxUnk  = e.UnknownIndex;
                if (e.IndexBufferIndex       > maxIdx) maxIdx  = e.IndexBufferIndex;
                if (e.VertexBufferIndex      > maxVtx) maxVtx  = e.VertexBufferIndex;
                if (e.MultiMaterialMeshIndex < minMmm) minMmm = e.MultiMaterialMeshIndex;
                if (e.UnknownIndex           < minUnk) minUnk  = e.UnknownIndex;
                if (e.IndexBufferIndex       < minIdx) minIdx  = e.IndexBufferIndex;
                if (e.VertexBufferIndex      < minVtx) minVtx  = e.VertexBufferIndex;
            }

            Console.WriteLine();
            Console.WriteLine($"[MeshReader] === SingleMaterialMeshes field statistics ({entries.Count} entries) ===");
            Console.WriteLine($"[MeshReader]   {"Field",-22}  {"Min",6}  {"Max",6}  {"Max+1",7}  Cross-reference");
            Console.WriteLine($"[MeshReader]   {new string('-', 85)}");
            Console.WriteLine($"[MeshReader]   {"MultiMaterialMeshIndex",-22}  {minMmm,6}  {maxMmm,6}  {maxMmm + 1,7}  MultiMaterialMeshes.Count   = {file.MultiMaterialMeshes?.Count ?? -1}");
            Console.WriteLine($"[MeshReader]   {"UnknownIndex",-22}  {minUnk,6}  {maxUnk,6}  {maxUnk + 1,7}  ??? (table not yet identified)");
            Console.WriteLine($"[MeshReader]   {"IndexBufferIndex",-22}  {minIdx,6}  {maxIdx,6}  {maxIdx + 1,7}  Index1DBufferHeaders.Count  = {file.Index1DBufferHeaders?.Count ?? -1}");
            Console.WriteLine($"[MeshReader]   {"VertexBufferIndex",-22}  {minVtx,6}  {maxVtx,6}  {maxVtx + 1,7}  Vertex1DBufferHeaders.Count = {file.Vertex1DBufferHeaders?.Count ?? -1}");

            return entries;
        }

        /// <summary>
        /// Parses Index1DBufferHeaders section. Each entry is 16 bytes (4 × uint32):
        ///   [0] StartIndex  — cumulative start in index stream (units = individual indices)
        ///   [1] IndexCount  — number of indices in this buffer slice
        ///   [2] Unknown     — not a simple stride×count; relationship TBD (see Index1DBufferHeader)
        ///   [3] Flags       — always 0xC0000001 in observed data; low bit may encode index width
        /// </summary>
        protected List<Model.Mesh.Index1DBufferHeader> ReadIndex1DBufferHeaders(Stream s, MeshFile file)
        {
            var section = file.SubHeader.Index1DBufferHeaders;
            var headers = new List<Model.Mesh.Index1DBufferHeader>((int)section.Count);

            s.Seek(section.Offset, SeekOrigin.Begin);

            var buffer = new byte[4];
            var buf2 = new byte[2];
            for (uint i = 0; i < section.Count; i++)
            {
                var h = new Model.Mesh.Index1DBufferHeader();

                s.Read(buffer, 0, 4); h.Offset = BitConverter.ToUInt32(buffer, 0);
                s.Read(buffer, 0, 4); h.Length = BitConverter.ToUInt32(buffer, 0);
                // Last 8 bytes treated as 4 × uint16
                s.Read(buf2, 0, 2); h.S0 = BitConverter.ToUInt16(buf2, 0);
                s.Read(buf2, 0, 2); h.S1 = BitConverter.ToUInt16(buf2, 0);
                s.Read(buf2, 0, 2); h.S2 = BitConverter.ToUInt16(buf2, 0);
                s.Read(buf2, 0, 2); h.S3 = BitConverter.ToUInt16(buf2, 0);

                headers.Add(h);
            }

            //// Print table for analysis
            //Console.WriteLine();
            //Console.WriteLine($"[MeshReader] === Index1DBufferHeaders ({headers.Count} entries) ===");
            //Console.WriteLine($"[MeshReader]   {"#",-5}  {"Offset",10}  {"Length",8}  {"S0(var)",8}  {"S1",6}  {"S2",6}  {"S3",10}  {"S0/Length",10}");
            //Console.WriteLine($"[MeshReader]   {new string('-', 80)}");
            //for (int i = 0; i < headers.Count; i++)
            //{
            //    var h = headers[i];
            //    string ratio = h.Length > 0 ? $"{(double)h.S0 / h.Length:F3}" : "-";
            //    Console.WriteLine($"[MeshReader]   {i,-5}  {h.Offset,10}  {h.Length,8}  {h.S0,8}  {h.S1,6}  {h.S2,6}  0x{h.S3:X4}  {ratio,10}");
            //}

            return headers;
        }

        /// <summary>
        /// Parses Vertex1DBufferHeaders section. Same 16-byte layout as Index1DBufferHeaders,
        /// with the key difference that S2 acts as a vertex format type index (0–3 observed)
        /// mapping into the VertexTypeNames string table.
        /// </summary>
        protected List<Vertex1DBufferHeader> ReadVertex1DBufferHeaders(Stream s, MeshFile file)
        {
            var section = file.SubHeader.Vertex1DBufferHeaders;
            var headers = new List<Vertex1DBufferHeader>((int)section.Count);

            s.Seek(section.Offset, SeekOrigin.Begin);

            var buffer = new byte[4];
            var buf2   = new byte[2];
            for (uint i = 0; i < section.Count; i++)
            {
                var h = new Vertex1DBufferHeader();

                s.Read(buffer, 0, 4); h.Offset = BitConverter.ToUInt32(buffer, 0);
                s.Read(buffer, 0, 4); h.Length = BitConverter.ToUInt32(buffer, 0);
                s.Read(buf2,   0, 2); h.S0     = BitConverter.ToUInt16(buf2, 0);
                s.Read(buf2,   0, 2); h.S1     = BitConverter.ToUInt16(buf2, 0);
                s.Read(buf2,   0, 2); h.S2     = BitConverter.ToUInt16(buf2, 0);
                s.Read(buf2,   0, 2); h.S3     = BitConverter.ToUInt16(buf2, 0);

                headers.Add(h);
            }

            //// Print table — S2 is the vertex format index; cross-verify S0 against stride × Length
            //Console.WriteLine();
            //Console.WriteLine($"[MeshReader] === Vertex1DBufferHeaders ({headers.Count} entries) ===");
            //Console.WriteLine($"[MeshReader]   {"#",-5}  {"Offset",10}  {"Length",8}  {"S0",6}  {"S2(fmt)",8}  {"Stride",8}  {"S0/Stride",10}  {"S0 == Stride×Len?",20}  FormatName");
            //Console.WriteLine($"[MeshReader]   {new string('-', 110)}");
            //for (int i = 0; i < headers.Count; i++)
            //{
            //    var h = headers[i];
            //    var fmt    = (file.VertexFormats != null && h.S2 < file.VertexFormats.Count)
            //                 ? file.VertexFormats[h.S2] : null;
            //    int stride = fmt?.Stride ?? 0;
            //    long expected = (long)stride * h.Length;
            //    string check = stride > 0
            //        ? (h.S0 == expected ? "✓" : $"✗ (expected {expected})")
            //        : "?";
            //    string fmtName = fmt != null
            //        ? fmt.TypeName[(fmt.TypeName.LastIndexOf('/') + 1)..]
            //        : $"[S2={h.S2} unknown]";
            //    Console.WriteLine($"[MeshReader]   {i,-5}  {h.Offset,10}  {h.Length,8}  {h.S0,6}  {h.S2,8}  {stride,8}  {(stride > 0 ? ((double)h.S0 / stride).ToString("F3") : "-"),10}  {check,-20}  {fmtName}");
            //}

            return headers;
        }

        /// <summary>
        /// Slices the Index1DBufferStreams section into per-header index arrays.
        ///
        /// Both Offset and Length in Index1DBufferHeader are in BYTES (verified: Σ Length == streamSize).
        /// Byte start of slice[i] = streamBase + header.Offset  (no stride multiplication).
        ///
        /// Index stride is 2 (16-bit / ushort) — confirmed by all observed max-index values fitting
        /// in uint16.  S2 == 0x0001 in every entry corroborates the 16-bit format.
        ///
        /// Result: file.IndexStride = 2, file.IndexBufferSlices[i] = ushort[] for header[i].
        /// </summary>
        protected List<ushort[]> ReadIndexBufferStreams(Stream s, MeshFile file)
        {
            var headers    = file.Index1DBufferHeaders;
            var streamInfo = file.SubHeader.Index1DBufferStreams;
            long streamBase = streamInfo.Offset;
            const int stride = 2; // 16-bit indices
            file.IndexStride = stride;

            // Sanity: Σ(Length) should equal streamSize since Length is in bytes
            long totalBytes = headers.Sum(h => (long)h.Length);
            if (totalBytes != streamInfo.Size)
                Console.WriteLine($"[MeshReader] WARNING: IndexStream size mismatch: Σ(Length)={totalBytes}, streamSize={streamInfo.Size}");

            var slices = new List<ushort[]>(headers.Count);
            var buf    = new byte[2];

            for (int i = 0; i < headers.Count; i++)
            {
                var h = headers[i];
                s.Seek(streamBase + (long)h.Offset, SeekOrigin.Begin); // Offset already in bytes

                int indexCount = (int)(h.Length / stride);
                var indices    = new ushort[indexCount];
                for (int j = 0; j < indexCount; j++)
                {
                    s.Read(buf, 0, 2);
                    indices[j] = BitConverter.ToUInt16(buf, 0);
                }
                slices.Add(indices);
            }

            //Console.WriteLine();
            //Console.WriteLine($"[MeshReader] === IndexBufferStreams (stride={stride}B, {slices.Count} slices) ===");
            //Console.WriteLine($"[MeshReader]   {"#",-5}  {"ByteOffset",12}  {"ByteLen",8}  {"IdxCount",9}  {"MaxIdx",8}");
            //Console.WriteLine($"[MeshReader]   {new string('-', 52)}");
            //for (int i = 0; i < slices.Count; i++)
            //{
            //    ushort max = slices[i].Length > 0 ? slices[i].Max() : (ushort)0;
            //    Console.WriteLine($"[MeshReader]   {i,-5}  {headers[i].Offset,12}  {headers[i].Length,8}  {slices[i].Length,9}  {max,8}");
            //}

            return slices;
        }

        /// <summary>
        /// Slices the Vertex1DBufferStreams section into per-header raw byte arrays.
        ///
        /// Both Offset and Length in Vertex1DBufferHeader are in BYTES (same convention as index headers).
        /// Byte start of slice[i] = streamBase + header.Offset  (no stride multiplication).
        /// Vertex count = Length / stride  (stride = VertexFormats[S2].Stride).
        ///
        /// Result: file.VertexBufferSlices[i] = raw bytes for header[i]; decode with VertexFormats[S2].
        /// </summary>
        protected List<byte[]> ReadVertexBufferStreams(Stream s, MeshFile file)
        {
            var headers    = file.Vertex1DBufferHeaders;
            var streamBase = file.SubHeader.Vertex1DBufferStreams.Offset;

            var slices = new List<byte[]>(headers.Count);

            for (int i = 0; i < headers.Count; i++)
            {
                var h      = headers[i];
                var fmt    = (file.VertexFormats != null && h.S2 < file.VertexFormats.Count)
                             ? file.VertexFormats[h.S2] : null;
                if (fmt == null)
                    throw new InvalidDataException(
                        $"[MeshReader] Vertex slice {i}: unknown format (S2={h.S2})");

                // Offset and Length are both in bytes
                s.Seek(streamBase + (long)h.Offset, SeekOrigin.Begin);

                var data = new byte[h.Length];
                int totalRead = 0;
                while (totalRead < data.Length)
                {
                    int read = s.Read(data, totalRead, data.Length - totalRead);
                    if (read == 0) break;
                    totalRead += read;
                }
                slices.Add(data);
            }

            Console.WriteLine();
            //Console.WriteLine($"[MeshReader] === VertexBufferStreams ({slices.Count} slices) ===");
            //Console.WriteLine($"[MeshReader]   {"#",-5}  {"ByteOffset",12}  {"ByteLen",10}  {"Stride",7}  {"VtxCount",9}");
            //Console.WriteLine($"[MeshReader]   {new string('-', 55)}");
            //for (int i = 0; i < slices.Count; i++)
            //{
            //    var h      = headers[i];
            //    var fmt    = file.VertexFormats[h.S2];
            //    int vtxCount = fmt.Stride > 0 ? (int)(h.Length / fmt.Stride) : 0;
            //    Console.WriteLine($"[MeshReader]   {i,-5}  {h.Offset,12}  {h.Length,10}  {fmt.Stride,7}  {vtxCount,9}");
            //}

            return slices;
        }

        protected MeshSubHeader ReadSubHeader(Stream ms)
        {
            var shead = new MeshSubHeader();

            var buffer = new byte[4];

            ms.Read(buffer, 0, buffer.Length);
            shead.MeshCount = BitConverter.ToUInt32(buffer, 0);

            shead.Dictionary = ReadSubHeaderEntryWithCount(ms);
            shead.VertexTypeNames = ReadSubHeaderEntryWithCount(ms);
            shead.MeshMaterial = ReadSubHeaderEntryWithCount(ms);

            shead.KeyedMeshSubPart = ReadSubHeaderEntryWithCount(ms);
            shead.KeyedMeshSubPartVectors = ReadSubHeaderEntryWithCount(ms);
            shead.MultiMaterialMeshes = ReadSubHeaderEntryWithCount(ms);
            shead.SingleMaterialMeshes = ReadSubHeaderEntryWithCount(ms);
            shead.Index1DBufferHeaders = ReadSubHeaderEntryWithCount(ms);
            shead.Index1DBufferStreams = ReadSubHeaderEntry(ms);
            shead.Vertex1DBufferHeaders = ReadSubHeaderEntryWithCount(ms);
            shead.Vertex1DBufferStreams = ReadSubHeaderEntry(ms);

            return shead;
        }

        protected MeshHeaderEntry ReadSubHeaderEntry(Stream s)
        {
            var entry = new MeshHeaderEntry();

            var buffer = new byte[4];

            s.Read(buffer, 0, buffer.Length);
            entry.Offset = BitConverter.ToUInt32(buffer, 0);

            s.Read(buffer, 0, buffer.Length);
            entry.Size = BitConverter.ToUInt32(buffer, 0);

            return entry;
        }

        protected MeshHeaderEntryWithCount ReadSubHeaderEntryWithCount(Stream s)
        {
            var entry = ReadSubHeaderEntry(s);

            var entryWithCount = new MeshHeaderEntryWithCount()
                {
                    Offset = entry.Offset,
                    Size = entry.Size
                };

            var buffer = new byte[4];
            s.Read(buffer, 0, buffer.Length);

            entryWithCount.Count = BitConverter.ToUInt32(buffer, 0);

            return entryWithCount;
        }

        protected MeshHeader ReadHeader(Stream ms)
        {
            var head = new MeshHeader();

            var buffer = new byte[4];

            ms.Read(buffer, 0, buffer.Length);

            if (BitConverter.ToUInt32(buffer, 0) != MeshMagic)
                throw new InvalidDataException("Wrong header magic");

            ms.Read(buffer, 0, buffer.Length);
            head.Platform = BitConverter.ToUInt32(buffer, 0);

            ms.Read(buffer, 0, buffer.Length);
            head.Version = BitConverter.ToUInt32(buffer, 0);

            ms.Read(buffer, 0, buffer.Length);
            head.FileSize = BitConverter.ToUInt32(buffer, 0);

            var chkSumBuffer = new byte[16];

            ms.Read(chkSumBuffer, 0, chkSumBuffer.Length);
            head.Checksum = chkSumBuffer;

            ms.Read(buffer, 0, buffer.Length);
            head.HeaderOffset = BitConverter.ToUInt32(buffer, 0);
            ms.Read(buffer, 0, buffer.Length);
            head.HeaderSize = BitConverter.ToUInt32(buffer, 0);

            ms.Read(buffer, 0, buffer.Length);
            head.ContentOffset = BitConverter.ToUInt32(buffer, 0);
            ms.Read(buffer, 0, buffer.Length);
            head.ContentSize = BitConverter.ToUInt32(buffer, 0);

            return head;
        }

        protected ObservableCollection<MeshContentFile> ReadMeshDictionary(Stream s, MeshFile f)
        {
            var files = new ObservableCollection<MeshContentFile>();
            var dirs = new List<EdataDir>();
            var endings = new List<long>();

            s.Seek(f.SubHeader.Dictionary.Offset, SeekOrigin.Begin);

            long dirEnd = f.SubHeader.Dictionary.Offset + f.SubHeader.Dictionary.Size;

            while (s.Position < dirEnd)
            {
                var buffer = new byte[4];
                s.Read(buffer, 0, 4);
                int fileGroupId = BitConverter.ToInt32(buffer, 0);

                if (fileGroupId == 0)
                {
                    var file = new MeshContentFile();
                    s.Read(buffer, 0, 4);
                    file.FileEntrySize = BitConverter.ToUInt32(buffer, 0);

                    var minp = new Point3D();
                    s.Read(buffer, 0, buffer.Length);
                    minp.X = BitConverter.ToSingle(buffer, 0);
                    s.Read(buffer, 0, buffer.Length);
                    minp.Y = BitConverter.ToSingle(buffer, 0);
                    s.Read(buffer, 0, buffer.Length);
                    minp.Z = BitConverter.ToSingle(buffer, 0);
                    file.MinBoundingBox = minp;

                    var maxp = new Point3D();
                    s.Read(buffer, 0, buffer.Length);
                    maxp.X = BitConverter.ToSingle(buffer, 0);
                    s.Read(buffer, 0, buffer.Length);
                    maxp.Y = BitConverter.ToSingle(buffer, 0);
                    s.Read(buffer, 0, buffer.Length);
                    maxp.Z = BitConverter.ToSingle(buffer, 0);
                    file.MaxBoundingBox = maxp;

                    s.Read(buffer, 0, buffer.Length);
                    file.Flags = BitConverter.ToUInt32(buffer, 0);

                    buffer = new byte[2];

                    s.Read(buffer, 0, buffer.Length);
                    file.MultiMaterialMeshIndex = BitConverter.ToUInt16(buffer, 0);

                    s.Read(buffer, 0, buffer.Length);
                    file.HierarchicalAseModelSkeletonIndex = BitConverter.ToUInt16(buffer, 0);

                    file.Name = Utils.ReadString(s);
                    file.Path = MergePath(dirs, file.Name);

                    if (file.Name.Length % 2 == 0)
                        s.Seek(1, SeekOrigin.Current);

                    files.Add(file);

                    while (endings.Count > 0 && s.Position == endings.Last())
                    {
                        dirs.Remove(dirs.Last());
                        endings.Remove(endings.Last());
                    }
                }
                else if (fileGroupId > 0)
                {
                    var dir = new EdataDir();

                    s.Read(buffer, 0, 4);
                    dir.FileEntrySize = BitConverter.ToInt32(buffer, 0);

                    if (dir.FileEntrySize != 0)
                        endings.Add(dir.FileEntrySize + s.Position - 8);
                    else if (endings.Count > 0)
                        endings.Add(endings.Last());

                    dir.Name = Utils.ReadString(s);

                    if (dir.Name.Length % 2 == 0)
                        s.Seek(1, SeekOrigin.Current);

                    dirs.Add(dir);
                }
            }

            return files;
        }

        protected string MergePath(IEnumerable<EdataDir> dirs, string fileName)
        {
            var b = new StringBuilder();

            foreach (var dir in dirs)
                b.Append(dir.Name);

            b.Append(fileName);

            return b.ToString();
        }
    }
}
