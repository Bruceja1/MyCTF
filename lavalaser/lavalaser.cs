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
using System.Diagnostics;
using MCGalaxy.DB;
using MCGalaxy.Blocks;
//using MyCTF.Events;

namespace lavalaser
{
    public sealed class LavaLaser : Plugin
    {
        public override string name { get { return "lavalaser"; } }
        public override string MCGalaxy_Version { get { return "1.9.5.1"; } }       
        static string author = "Bruceja"; // This name is used to determine who to send debug text to
        public override string creator { get { return author; } }

        static BlockID igniteBlock = 13;       
        static BlockID lavaLaserBlock = 11; // Block that the laser is made out of
        static ushort maxLaserLength = 6;
        static double cooldown = 0.7;
        static Dictionary<int, Player> laserBlockDict = new Dictionary<int, Player>();
        const string laserExtrasKey = "LASER_DATA";

        public override void Load(bool startup)
        {
            OnBlockChangedEvent.Register(OnBlockPlaced, Priority.Low);
            OnBlockChangingEvent.Register(OnBlockPlacing, Priority.Low);
            OnBlockHandlersUpdatedEvent.Register(OnBlockHandlersUpdated, Priority.Low);
            OnLevelLoadedEvent.Register(HandleLevelLoaded, Priority.Low);
            OnPlayerDyingEvent.Register(HandlePlayerDying, Priority.Low);
        }
        public override void Unload(bool shutdown)
        {
            OnBlockChangedEvent.Unregister(OnBlockPlaced);
            OnBlockChangingEvent.Unregister(OnBlockPlacing);
            OnBlockHandlersUpdatedEvent.Unregister(OnBlockHandlersUpdated);
            OnLevelLoadedEvent.Unregister(HandleLevelLoaded);
            OnPlayerDyingEvent.Unregister(HandlePlayerDying);
        }

        private static void HandlePlayerDying(Player p, ushort cause, ref bool cancel)
        {
            if (cause != lavaLaserBlock)
            {
                return;
            }
            cancel = true;
            Vec3S32 pos = p.Pos.FeetBlockCoords;
            int index = p.level.PosToInt((ushort)pos.X, (ushort)pos.Y, (ushort)pos.Z);
            if (!laserBlockDict.ContainsKey(index))
            {
                return;
            }
            OnWeaponContactEvent.Call(laserBlockDict[index], p);
        }
        private static void OnBlockPlacing(Player p, ushort x, ushort y, ushort z, ushort block, bool placing, ref bool cancel)
        {   
            if (block != igniteBlock || !placing)
            {
                return;
            }
            BlockID blockAt = p.level.GetBlock(x, y, z);          
            if (blockAt != Block.Air || IsOnCooldown(p))
            {                
                cancel = true;              
            }
            p.RevertBlock(x, y, z);
        }
        private static void OnBlockPlaced(Player p, ushort x, ushort y, ushort z, ChangeResult result)
        {
            BlockID block = p.level.GetBlock(x, y, z);
            if (block != igniteBlock)
            {
                return;
            }

            // Other players can't see the ignite block
            foreach (Player player in PlayerInfo.Online.Items)
            {
                if (player != p)
                {
                    player.SendBlockchange(x, y, z, Block.Air);
                }
            }
            DoLaser(p, x, y, z);
            p.Extras[laserExtrasKey] = DateTime.UtcNow;
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

        private static bool IsOnCooldown(Player p)
        {
            if (!p.Extras.Contains(laserExtrasKey))
            {
                p.Extras[laserExtrasKey] = DateTime.UtcNow;
                return false;
            }

            // Source: https://www.bytehide.com/blog/datetime-now-vs-datetime-utcnow-csharp
            DateTime startTime = (DateTime)p.Extras[laserExtrasKey];
            DateTime endTime = DateTime.UtcNow;
            TimeSpan elapsedTime = endTime - startTime;

            if (elapsedTime < TimeSpan.FromSeconds(cooldown))
            {
                return true;
            }

            return false;
        }

        private static void DoLaser(Player p, ushort x, ushort y, ushort z)
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
                BlockID nextBlock = p.level.GetBlock((ushort)(pos.X + incrementX), (ushort)(pos.Y + incrementY), (ushort)(pos.Z + incrementZ));
                //if (nextBlock == Block.Air || nextBlock == lavaLaserBlock || nextBlock == igniteBlock)
                p.level.AddUpdate(index, lavaLaserBlock);
                laserBlockDict[index] = p;
                if (nextBlock != Block.Air)
                {
                    break;
                }
                pos.X = (ushort)(pos.X + incrementX);
                pos.Y = (ushort)(pos.Y + incrementY);
                pos.Z = (ushort)(pos.Z + incrementZ);
            }
        }

        private static void OnBlockHandlersUpdated(Level lvl, BlockID block)
        {
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
            laserBlockDict.Remove(C.Index);
        }

        private static void HandleLevelLoaded(Level lvl)
        {
            lvl.PhysicsHandlers[lavaLaserBlock] = DoCleanup;
            // Makes sure that, if the igniteBlock is gravel, the block does not fall.
            lvl.PhysicsHandlers[igniteBlock] = delegate (Level level, ref PhysInfo C) { return; };
        }
    }
}