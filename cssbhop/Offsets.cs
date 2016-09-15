namespace cssbhop
{
    /// <summary>
    /// Static values and offsets.
    /// All are updated for 15/09/2016 (dd/mm/yy of course); might break with updates.
    /// </summary>
    class Offsets
    {
        // Taken from SourceMod (addons/sourcemod/scripting/include/entity_prop_stocks.inc).
        public enum MoveType
        {
            MOVETYPE_NONE = 0,          /**< never moves */
            MOVETYPE_ISOMETRIC,         /**< For players */
            MOVETYPE_WALK,              /**< Player only - moving on the ground */
            MOVETYPE_STEP,              /**< gravity, special edge handling -- monsters use this */
            MOVETYPE_FLY,               /**< No gravity, but still collides with stuff */
            MOVETYPE_FLYGRAVITY,        /**< flies through the air + is affected by gravity */
            MOVETYPE_VPHYSICS,          /**< uses VPHYSICS for simulation */
            MOVETYPE_PUSH,              /**< no clip to world, push and crush */
            MOVETYPE_NOCLIP,            /**< No gravity, no collisions, still do velocity/avelocity */
            MOVETYPE_LADDER,            /**< Used by players only when going onto a ladder */
            MOVETYPE_OBSERVER,          /**< Observer movement, depends on player's observer mode */
            MOVETYPE_CUSTOM             /**< Allows the entity to describe its own physics */
        };

        /// <summary>
        /// client.dll + this is the player struct.
        /// </summary>
        public static int LocalPlayer = 0x4C6708;

        /// <summary>
        /// client.dll + this is used to force +jump.
        /// </summary>
        public static int JumpAddress = 0x4F3B3C;

        /// <summary>
        /// Is the pause menu open? (vguimatsurface.dll + this)
        /// </summary>
        public static int PauseMenu = 0x135008;

        /// <summary>
        /// Is the chat open?
        /// </summary>
        public static int ChatOpen = 0x106813B4;

        /// <summary>
        /// Life status; 25600 is alive.
        /// </summary>
        public static int m_lifestate = 0x93;

        /// <summary>
        /// Health, used for testing and finding LocalPlayer.
        /// </summary>
        public static int m_iHealth = 0x94;

        /// <summary>
        /// Team number, so we don't trigger bhop in spectator mode.
        /// </summary>
        public static int m_iTeam = 0x9C;

        /// <summary>
        /// Player flags.
        /// </summary>
        public static int m_fFlags = 0x350;

        /// <summary>
        /// Movetype. See enum MoveType for possible values.
        /// </summary>
        public static int m_MoveType = 0x178;

        /// <summary>
        /// Ground entity. -1 means not on ground, other values are brush entities.
        /// </summary>
        public static int m_hGroundEntity = 0x254;
    }
}
