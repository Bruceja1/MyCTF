using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MCGalaxy;
using MCGalaxy.Blocks.Physics;
using MCGalaxy.Events.LevelEvents;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Levels.IO;
using MCGalaxy.Maths;
using MCGalaxy.Tasks;
using BlockID = System.UInt16;

namespace lavalaser
{
    public sealed class LavaLaser : Plugin
    {
        public override string name { get { return "lavalaser"; } }
        public override string MCGalaxy_Version { get { return "1.9.5.1"; } }
        //This name is used to determine who to send debug text to
        static string author = "Bruceja";
        public override string creator { get { return author; } }

        //The level we want to add a custom physics block to.
        static string physicsLevelName = "bruceja5";
        //Block that sets off the laser
        static BlockID igniteBlock = 13;
        //Block that the laser is made out of
        static BlockID lavaLaserBlock = 11;
        static ushort maxLaserLength = 8;
        static double cooldown = 0.2;

        const string laserExtrasKey = "LASER_DATA";
         
        public override void Load(bool startup)
        {
            OnBlockChangedEvent.Register(OnBlockPlaced, Priority.Low);
            Level[] levels = LevelInfo.Loaded.Items;
            foreach (Level lvl in levels)
            {
                if (lvl.name == physicsLevelName)
                {
                    lvl.PhysicsHandlers[lavaLaserBlock] = DoCleanup;
                }
            }
            OnBlockHandlersUpdatedEvent.Register(OnBlockHandlersUpdated, Priority.Low);
        }
        public override void Unload(bool shutdown)
        {
            OnBlockChangedEvent.Unregister(OnBlockPlaced);
            OnBlockHandlersUpdatedEvent.Unregister(OnBlockHandlersUpdated);
        }
             
        private static void OnBlockPlaced(Player p, ushort x, ushort y, ushort z, ChangeResult result)
        {
            int blockIndex = p.level.PosToInt(x, y, z);
            List<int> laserBlockIndexes = new List<int>();

            if (!p.Extras.Contains(laserExtrasKey))
            {
                p.Extras[laserExtrasKey] = DateTime.Now;
            }

            if (IsOnCooldown(p))
            {
                p.level.AddUpdate(blockIndex, Block.Air);
                return;
            }

            //if (result == ChangeResult.Modified)
            
            BlockID newBlock = p.level.GetBlock(x, y, z);
            if (newBlock != igniteBlock)
            {
                return;
            }

            DoLaser(p, laserBlockIndexes, x, y ,z);

            KillEnemy(p, laserBlockIndexes);

            p.Extras[laserExtrasKey] = DateTime.Now;

            /*
            if (newBlock != Block.Air)
            {
                p.Message("{4} placed block ID {0} at ({1}, {2}, {3})", newBlock, x, y, z, p.ColoredName);
                Logger.Log(LogType.UserActivity, $"{p.ColoredName} placed block {newBlock} at {x}, {y}, {z}");
            } 
            */   
        }

        private static string GetPlayerDirection(Player p, int yaw)
        {
            string direction;
                    
            if (yaw >= 223 || yaw <= 31)
            {
                direction = "North";
            }

            else if (yaw >= 32 && yaw <= 95)
            {
                direction = "East";
            }

            else if (yaw >= 95 && yaw <= 158)
            {
                direction = "South";
            }

            else
            {
                direction = "West";
            }           

            return direction;
        }

        private static void KillEnemy(Player p, List<int> blockIndexes)
        {
            foreach (int blockPosition in blockIndexes)
            {
                foreach (Player opponent in p.level.players)
                {
                    int opponentLegPos = p.level.PosToInt((ushort)opponent.Pos.BlockCoords.X, (ushort)opponent.Pos.BlockCoords.Y, (ushort)opponent.Pos.BlockCoords.Z);
                    int opponentHeadPos = p.level.IntOffset(opponentLegPos, 0, -1, 0);

                    if (opponentLegPos == blockPosition || opponentHeadPos == blockPosition)
                    {
                        p.level.Message($"{opponent.ColoredName} &3was killed by {p.ColoredName}!");
                        opponent.SendPosition(p.level.SpawnPos, opponent.Rot);

                    }
                }
            }
        }

        private static bool IsOnCooldown(Player p)
        {  
            DateTime startTime = (DateTime)p.Extras[laserExtrasKey];
            DateTime endTime = DateTime.Now;
            TimeSpan elapsedTime = endTime - startTime;
            p.Message(elapsedTime.TotalSeconds.ToString());

            if (elapsedTime.Seconds < cooldown)
            {               
                return true;
            }

            return false;
        }

        private static void DoLaser(Player p, List<int> laserBlockIndexes, ushort x, ushort y, ushort z)
        {
            int index = p.level.PosToInt(x, y, z);
            Vec3U16 pos = new Vec3U16();
            pos.X = x; pos.Y = y; pos.Z = z;

            //Check in which direction the laser should be fired
            string direction = GetPlayerDirection(p, p.Rot.RotY);
            int incrementX = 0;
            int incrementY = 0;
            int incrementZ = 0;
            switch (direction)
            {
                case "North":
                    incrementZ = -1;
                    break;
                case "East":
                    incrementX = 1;
                    break;
                case "South":
                    incrementZ = 1;
                    break;

                default:
                    incrementX = -1;
                    break;
            }

            // Place line of lava blocks
            for (int i = 0; i < maxLaserLength; i++)
            {
                index = p.level.PosToInt(pos.X, pos.Y, pos.Z);

                // Laser will be interrupted if there is a block in front of it                      
                if (p.level.GetBlock((ushort)(pos.X + incrementX), (ushort)(pos.Y + incrementY), (ushort)(pos.Z + incrementZ)) != Block.Air)
                {
                    p.level.AddUpdate(index, lavaLaserBlock);
                    laserBlockIndexes.Add(index);
                    break;
                }

                p.level.AddUpdate(index, lavaLaserBlock);
                laserBlockIndexes.Add(index);

                pos.X = (ushort)(pos.X + incrementX);
                pos.Y = (ushort)(pos.Y + incrementY);
                pos.Z = (ushort)(pos.Z + incrementZ);
            }
        }

        private static void OnBlockHandlersUpdated(Level lvl, BlockID block)
        {
            if (lvl.name != physicsLevelName)
            {
                return;
            }
            if (block != lavaLaserBlock)
            {
                return;
            }

            lvl.PhysicsHandlers[lavaLaserBlock] = DoCleanup;
        }

        private static void DoCleanup(Level lvl, ref PhysInfo C)
        {
            // Remove lava block
            lvl.AddUpdate(C.Index, Block.Air, default(PhysicsArgs));
        }

        static void MsgDebugger(string message, params object[] args)
        {
            Player debugger = PlayerInfo.FindExact(LavaLaser.author); if (debugger == null) { return; }
            debugger.Message(message, args);
        }
    }
}

