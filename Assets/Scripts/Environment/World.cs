using System;
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
        
        private class ChunkNode : PriorityQueueNode, IEquatable<ChunkNode> { public int3 chunkPosition; public bool Equals(ChunkNode other) => chunkPosition.Equals(other!.chunkPosition); }
        
        private Dictionary<int3, Chunk> m_Chunks = new();
        private int3 m_LastTargetChunkPosition = int.MinValue;
        private PriorityQueue<ChunkNode> m_GenerateChunksQueue = new(233333);
        private SharedData m_SharedData;
        
        public int UpdatingChunks { get; set; }

        public bool CanUpdate => UpdatingChunks <= maxGenerateChunksInFrame; // Limit the update rate

        private void Awake()
        {
            m_SharedData = new SharedData();
            m_SharedData.Generate();
            Shader.SetGlobalInt(ShaderIDs.AtlasX, Shared.AtlasSize.x);
            Shader.SetGlobalInt(ShaderIDs.AtlasY, Shared.AtlasSize.y);
            Shader.SetGlobalVector(ShaderIDs.AtlasRec, new Vector4(1.0f / Shared.AtlasSize.x, 1.0f / Shared.AtlasSize.y));
        }
        
        private void Update()
        {
            m_SharedData.Update();
            
            var targetPosition = target.position.ToChunk(chunkSize);

            if (m_LastTargetChunkPosition.Equals(targetPosition)) return;

            // Make sure its not out of range;
            foreach (var chunkNode in m_GenerateChunksQueue)
            {
                if (chunkNode == null) return;
                var deltaPosition = targetPosition - chunkNode.chunkPosition;
                if (chunkSpawnSize.x < Mathf.Abs(deltaPosition.x) || chunkSpawnSize.y < Mathf.Abs(deltaPosition.y) || chunkSpawnSize.y < Mathf.Abs(deltaPosition.z))
                {
                    m_GenerateChunksQueue.Remove(chunkNode);
                    continue;
                }

                m_GenerateChunksQueue.UpdatePriority(chunkNode, math.lengthsq(deltaPosition));
            }
            
            for (int x = targetPosition.x - chunkSpawnSize.x, xMax = targetPosition.x + chunkSpawnSize.x; x <= xMax; x++)
                for (int z = targetPosition.z - chunkSpawnSize.y, zMax = targetPosition.z + chunkSpawnSize.y; z <= zMax; z++)
                {
                    var chunkPosition = new int3(x, 0, z);

                    if (m_Chunks.ContainsKey(chunkPosition)) continue;

                    var newNode = new ChunkNode { chunkPosition = chunkPosition };

                    if (m_GenerateChunksQueue.Contains(newNode)) continue;
                    
                    m_GenerateChunksQueue.Enqueue(newNode, math.lengthsq(targetPosition - chunkPosition)); // 往Queue添加Node
                }
            
            m_LastTargetChunkPosition = targetPosition;
        }

        private void LateUpdate()
        {
            int numChunks = 0;
            while (m_GenerateChunksQueue.Count != 0)
            {
                if (numChunks >= maxGenerateChunksInFrame) return;

                var chunkPosition = m_GenerateChunksQueue.Dequeue().chunkPosition;

                GenerateChunk(chunkPosition);
                numChunks++;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(target.position, new Vector3(chunkSize.x * chunkSpawnSize.x * 2, chunkSize.y, chunkSize.z * chunkSpawnSize.y * 2));
        }

        private Chunk GenerateChunk(int3 chunkPosition)
        {
            if (m_Chunks.ContainsKey(chunkPosition)) return m_Chunks[chunkPosition];

            var chunkGameObject = new GameObject(chunkPosition.ToString())
            {
                transform = {
                    parent = transform, position = chunkPosition.ToWorld(chunkSize)
                }
            };

            var newChunk = chunkGameObject.AddComponent<Chunk>();
            newChunk.InitChunk(chunkPosition, chunkMaterial, chunkSize);
            newChunk.OnChunkUpdate += () =>
            {
                for (int x = chunkPosition.x - 1, xMax = chunkPosition.x + 1; x <= xMax; x++)
                    for (int z = chunkPosition.z - 1, zMax = chunkPosition.z + 1; z <= zMax; z++)
                    {
                        var neighborChunkPosition = new int3(x, chunkPosition.y, z);
                        if (m_Chunks.TryGetValue(neighborChunkPosition, out Chunk neighborChunk))
                        {
                            if (!neighborChunk.Initialized)
                                return false;
                        }
                        else return false;
                    }

                return true;
            };

            m_Chunks.Add(chunkPosition, newChunk);
            return newChunk;
        }

        /// <summary> Get Chunk By WorldPos </summary>
        public bool GetChunk(Vector3 worldPosition, out Chunk chunk)
        {
            var chunkPosition = worldPosition.ToChunk(chunkSize);
            return m_Chunks.TryGetValue(chunkPosition, out chunk);
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

                                if (m_Chunks.TryGetValue(neighborChunkPosition, out Chunk neighborChunk))
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
                        neighborBlocks.Add(m_Chunks.TryGetValue(neighborChunkPosition, out Chunk chunk) ? chunk.Blocks : null);
                    }

            return neighborBlocks;
        }

        public List<Chunk> GetNeighborChunks(int3 chunkPosition, int numNeighbor)
        {
            var neighborChunks = new List<Chunk>();
            
            for (int x = chunkPosition.x - 1, xMax = chunkPosition.x + numNeighbor; x <= xMax; x++)
                for (int y = chunkPosition.y - numNeighbor, yMax = chunkPosition.y + numNeighbor; y <= yMax; y++)
                    for (int z = chunkPosition.z - numNeighbor, zMax = chunkPosition.z + numNeighbor; z <= zMax; z++)
                    {
                        var neighborChunkPosition = new int3(x, y, z);
                        neighborChunks.Add(m_Chunks.TryGetValue(neighborChunkPosition, out Chunk chunk) ? chunk : null);
                    }

            return neighborChunks;
        }
    }
}