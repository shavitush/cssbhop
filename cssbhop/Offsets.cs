namespace cssbhop
{
	/// <summary>
	/// Const values and offsets.
	/// All are updated for 15/09/2016 (dd/mm/yy of course); might break with updates.
	/// </summary>
	internal class Offsets
	{
		#region Source Engine enumerators
		// Taken from SourceMod (addons/sourcemod/scripting/include/entity_prop_stocks.inc).
		public enum MoveTypes
		{
			MovetypeNone = 0,          /**< never moves */
			MovetypeIsometric,         /**< For players */
			MovetypeWalk,              /**< Player only - moving on the ground */
			MovetypeStep,              /**< gravity, special edge handling -- monsters use this */
			MovetypeFly,               /**< No gravity, but still collides with stuff */
			MovetypeFlygravity,        /**< flies through the air + is affected by gravity */
			MovetypeVphysics,          /**< uses VPHYSICS for simulation */
			MovetypePush,              /**< no clip to world, push and crush */
			MovetypeNoclip,            /**< No gravity, no collisions, still do velocity/avelocity */
			MovetypeLadder,            /**< Used by players only when going onto a ladder */
			MovetypeObserver,          /**< Observer movement, depends on player's observer mode */
			MovetypeCustom             /**< Allows the entity to describe its own physics */
		}
		#endregion

		#region Memory addresses
		/// <summary>
		/// client.dll + this is the player struct.
		/// </summary>
		public const int LocalPlayer = 0x4C6708;

		/// <summary>
		/// client.dll + this is used to force +jump.
		/// </summary>
		public const int JumpAddress = 0x4F3B3C;

		/// <summary>
		/// Is the pause menu open? (vguimatsurface.dll + this)
		/// </summary>
		public const int PauseMenu = 0x135008;

		/// <summary>
		/// Is the chat open?
		/// </summary>
		public const int ChatOpen = 0x106813B4;
		#endregion

		#region Memory offsets

		/// <summary>
		/// Life status; 25600 is alive.
		/// </summary>
		public static int Lifestate { get; } = 0x93;

		/// <summary>
		/// Health, used for testing and finding LocalPlayer.
		/// </summary>
		public static int Health { get; } = 0x94;

		/// <summary>
		/// Team number, so we don't trigger bhop in spectator mode.
		/// </summary>
		public static int Team { get; } = 0x9C;

		/// <summary>
		/// Player flags.
		/// </summary>
		public static int Flags { get; } = 0x350;

		/// <summary>
		/// Movetype. See enum MoveTypes for possible values.
		/// </summary>
		public static int MoveType { get; } = 0x178;

		/// <summary>
		/// Ground entity. -1 means not on ground, other values are brush entities.
		/// </summary>
		public static int GroundEntity { get; } = 0x254;

		#endregion
	}
}
