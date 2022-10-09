using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using System.Reflection;
using UnityEngine;

namespace KarlsonLevels
{
	[HarmonyPatch(typeof(PlayerMovement), "Movement")]
	class PlayerMovementPatch
	{
		static bool Prefix(PlayerMovement __instance) {
			if (Main.editMode)
			{
				float x = Input.GetAxisRaw("Horizontal");
				float z = Input.GetAxisRaw("Vertical");
				float y = 0;
				if (Input.GetButton("Pickup")) y = 1f;
				if (Input.GetButton("Drop")) y = -1f;
				if (Main.movableObj == 0)
				{
					__instance.gameObject.transform.position += Camera.main.transform.forward * z + Camera.main.transform.right * x;
					__instance.rb.velocity = Vector3.zero;
				}
				else if (Main.MovementMode == Main.MoveModeEnum.movement)
				{
					Vector3 movReltiveToCam = Camera.main.transform.forward * z + Camera.main.transform.right * x;
					Main.Level[Main.IdToIndex(Main.movableObj)].Object.transform.position += new Vector3(movReltiveToCam.x, y, movReltiveToCam.z);
				}
				else if (Main.MovementMode == Main.MoveModeEnum.scale)
				{
					Main.Level[Main.IdToIndex(Main.movableObj)].Object.transform.localScale += new Vector3(x, y, z);
				}
				else // rotation ofc
                {
					Main.Level[Main.IdToIndex(Main.movableObj)].Object.transform.eulerAngles += new Vector3(x, y, z);

				}
				return false;
			}
			else return true;
		}
	}
}
