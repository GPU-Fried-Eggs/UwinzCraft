using System.Collections.Generic;
using Environment.Data;
using Unity.Mathematics;
using UnityEngine;
using Utilities;

namespace Environment
{
    public class World : MonoSingleton<World>
    {
        [SerializeField] private Transform target;
        [SerializeField] private int3 chunkSize = new int3(16, 128, 16);
        [SerializeField] private int2 chunkSpawnSize = 8;
        [SerializeField] private Material[] chunkMaterial;
        [SerializeField] private int maxGenerateChunksInFrame = 1;
        
        private class ChunkNode : PriorityQueueNode { public int3 chunkPosition; }
        
        private Dictionary<int3, Chunk> chunks = new();
        private int3 m_LastTargetChunkPosition = int.MinValue;
        private PriorityQueue<ChunkNode> generateChunkQueue = new(100000);
        private SharedData sharedData;
        
        public int UpdatingChunks { get; set; }

        public bool CanUpdate => UpdatingChunks <= maxGenerateChunksInFrame; // Limit the update rate

        private void Awake()
        {
            sharedData = new SharedData();
            sharedData.Generate();
            Shader.SetGlobalInt(ShaderIDs.AtlasX, Shared.AtlasSize.x);
            Shader.SetGlobalInt(ShaderIDs.AtlasY, Shared.AtlasSize.y);
            Shader.SetGlobalVector(ShaderIDs.AtlasRec, new Vector4(1.0f / Shared.AtlasSize.x, 1.0f / Shared.AtlasSize.y));
        }
        
        private void Update()
        {
            sharedData.Update();
            
            if (target == null) return; // safety check

            var targetPosition = target.position.ToChunk(chunkSize);

            if (m_LastTargetChunkPosition.Equals(targetPosition)) return;

            // Make sure its not out of range;
            foreach (var chunkNode in generateChunkQueue)
            {
                if (chunkNode == null) return;
                var deltaPosition = targetPosition - chunkNode.chunkPosition;
                if (chunkSpawnSize.x < Mathf.Abs(deltaPosition.x) || chunkSpawnSize.y < Mathf.Abs(deltaPosition.y) || chunkSpawnSize.y < Mathf.Abs(deltaPosition.z))
                {
                    generateChunkQueue.Remove(chunkNode);
                    continue;
                }

                generateChunkQueue.UpdatePriority(chunkNode, math.lengthsq(deltaPosition));
            }

            // Direct generate y once could ignore the bug of no object culling above
            for (int x = targetPosition.x - chunkSpawnSize.x, xMax = targetPosition.x + chunkSpawnSize.x; x <= xMax; x++)
                for (int z = targetPosition.z - chunkSpawnSize.y, zMax = targetPosition.z + chunkSpawnSize.y; z <= zMax; z++)
                {
                    var chunkPosition = new int3(x, 0, z);

                    if (chunks.ContainsKey(chunkPosition)) continue;

                    var newNode = new ChunkNode { chunkPosition = chunkPosition };

                    if (generateChunkQueue.Contains(newNode)) continue;

                    var deltaPosition = targetPosition - chunkPosition;
                    generateChunkQueue.Enqueue(newNode, math.lengthsq(deltaPosition)); // 往Queue添加Node
                }
            
            m_LastTargetChunkPosition = targetPosition;
        }

        private void LateUpdate()
        {
            int numChunks = 0;
            while (generateChunkQueue.Count != 0)
            {
                if (numChunks >= maxGenerateChunksInFrame) return;

                var chunkPosition = generateChunkQueue.Dequeue().chunkPosition;

                GenerateChunk(chunkPosition);
                numChunks++;
            }
        }

        private Chunk GenerateChunk(int3 chunkPosition)
        {
            if (chunks.ContainsKey(chunkPosition)) return chunks[chunkPosition];

            var chunkGameObject = new GameObject(chunkPosition.ToString());
            chunkGameObject.transform.SetParent(transform);
            chunkGameObject.transform.position = chunkPosition.ToWorld(chunkSize);

            var newChunk = chunkGameObject.AddComponent<Chunk>();
            newChunk.InitChunk(chunkPosition, chunkMaterial, chunkSize);
            newChunk.OnChunkUpdate += () =>
            {
                for (int x = chunkPosition.x - 1, xMax = chunkPosition.x + 1; x <= xMax; x++)
                    for (int z = chunkPosition.z - 1, zMax = chunkPosition.z + 1; z <= zMax; z++)
                    {
                        var neighborChunkPosition = new int3(x, chunkPosition.y, z);
                        if (chunks.TryGetValue(neighborChunkPosition, out Chunk neighborChunk))
                        {
                            if (!neighborChunk.Initialized)
                                return false;
                        }
                        else return false;
                    }

                return true;
            };

            chunks.Add(chunkPosition, newChunk);
            return newChunk;
        }

        /// <summary> Get Chunk By WorldPos </summary>
        public bool GetChunk(Vector3 worldPosition, out Chunk chunk)
        {
            var chunkPosition = worldPosition.ToChunk(chunkSize);
            return chunks.TryGetValue(chunkPosition, out chunk);
        }

        public bool GetBlock(Vector3 worldPosition, out Block block)
        {
            if (GetChunk(worldPosition, out Chunk chunk))
            {
                var chunkPosition = worldPosition.ToChunk(chunkSize);
                var gridPosition = worldPosition.ToGrid(chunkPosition, chunkSize);
                if (chunk.GetBlock(gridPosition, out block)) return true;
            }

            block = Block.Empty;
            return false;
        }

        public bool IsAir(Vector3 worldPosition)
        {
            if (GetBlock(worldPosition, out Block block))
                return block.type == BlockType.Air;

            return false;
        }

        public bool SetBlock(Vector3 worldPosition, BlockType type)
        {
            if (GetChunk(worldPosition, out Chunk chunk))
            {
                var chunkPosition = worldPosition.ToChunk(chunkSize);
                var gridPosition = worldPosition.ToGrid(chunkPosition, chunkSize);

                if (gridPosition.Equals(target.position.ToGrid(chunkPosition, chunkSize)) ||
                    gridPosition.Equals(target.position.ToGrid(chunkPosition, chunkSize) + new int3(0, 1, 0)))
                    return false;

                if (chunk.SetBlock(gridPosition, type))
                {
                    // Check Chunk Border
                    for (int x = -1; x <= 1; x++)
                        for (int y = -1; y <= 1; y++)
                            for (int z = -1; z <= 1; z++)
                            {
                                if ((gridPosition + new int3(x, y, z)).BoundaryCheck(chunkSize)) continue;

                                var neighborChunkPosition = (worldPosition + new Vector3(x, y, z)).ToChunk(chunkSize);
                                if (chunkPosition.Equals(neighborChunkPosition)) continue;

                                if (chunks.TryGetValue(neighborChunkPosition, out Chunk neighborChunk))
                                    neighborChunk.NeighborChunkIsChanged();
                            }

                    return true;
                }
            }
            return false;
        }

        public List<Block[]> GetNeighborBlocks(int3 chunkPosition, int numNeighbor)
        {
            var neighborBlocks = new List<Block[]>();
            
            for (int x = chunkPosition.x - numNeighbor, xMax = chunkPosition.x + numNeighbor; x <= xMax; x++)
                for (int y = chunkPosition.y - numNeighbor, yMax = chunkPosition.y + numNeighbor; y <= yMax; y++)
                    for (int z = chunkPosition.z - numNeighbor, zMax = chunkPosition.z + numNeighbor; z <= zMax; z++)
                    {
                        var neighborChunkPosition = new int3(x, y, z);
                        neighborBlocks.Add(chunks.TryGetValue(neighborChunkPosition, out Chunk chunk) ? chunk.Blocks : null);
                    }

            return neighborBlocks;
        }
    }
}