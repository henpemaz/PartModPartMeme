using System;
using System.Collections.Generic;

namespace ConcealedGarden
{
	internal class CGAmbienceFix
	{
		internal static void Apply()
		{
			On.VirtualMicrophone.NewRoom += VirtualMicrophone_NewRoom;
		}

		private static void VirtualMicrophone_NewRoom(On.VirtualMicrophone.orig_NewRoom orig, VirtualMicrophone self, Room room)
		{
			orig(self, room);
			// Remove sounds that shouldn't be present in the damn room.
			// Vanilla only checks agains the template and so overriden ambiences stay and get duplicated

			/* Vanilla bug in VirtualMicrophone.NewRoom
			 * -On entering a new room it'll build a list of stuff that's currently playing that's also in the new rooms' template, and get rid of the rest
			 * - It'll check against the members of that list for anything that is missing for the room and create that, and doesn't delete any template stuff that was overriden.
			 * - Overriden ambiences play twice, both the template's and the overriden version, if you entered the room with the ambience from the template already playing from the previous room.
			 * */
			for (int i = self.ambientSoundPlayers.Count - 1; i >= 0; i--)
			{
				bool flag = false;
				for (int j = 0; j < room.roomSettings.ambientSounds.Count; j++)
				{
					if (self.ambientSoundPlayers[i].aSound == room.roomSettings.ambientSounds[j])
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					self.ambientSoundPlayers[i].Destroy();
					self.ambientSoundPlayers.RemoveAt(i);
				}
			}
		}
	}
}