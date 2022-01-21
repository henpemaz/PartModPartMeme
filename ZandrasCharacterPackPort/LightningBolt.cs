using System;
using System.Collections.Generic;
using RWCustom;
using UnityEngine;

namespace ZandrasCharacterPackPort
{

	public class LightningBolt : UpdatableAndDeletable, IDrawable
	{
		public LightningBolt(Vector2 startPoint, Vector2 endPoint, float lifetime, Color color)
		{
			this.startPoint = startPoint;
			this.endPoint = endPoint;
			this.maxLifetime = lifetime;
			this.lifetime = lifetime;
			this.stuckAt = startPoint + UnityEngine.Random.insideUnitCircle * 5f;
			this.handle = this.stuckAt + Custom.RNV() * Mathf.Lerp(30f, 100f, UnityEngine.Random.value);
		}

		public override void Update(bool eu)
		{
			base.Update(eu);
			this.lifetime -= 0.016666668f;
			if (this.lifetime <= 0f)
			{
				this.Destroy();
			}
		}

		public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			sLeaser.sprites = new FSprite[1];
			new List<TriangleMesh.Triangle>();
			sLeaser.sprites[0] = TriangleMesh.MakeLongMesh(10, false, false);
			sLeaser.sprites[0].color = new Color(1f, 1f, 1f, 1f);
			this.AddToContainer(sLeaser, rCam, null);
		}

		public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			sLeaser.RemoveAllSpritesFromContainer();
			if (newContatiner == null)
			{
				newContatiner = rCam.ReturnFContainer("Midground");
			}
			newContatiner.AddChild(sLeaser.sprites[0]);
		}

		public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			if (base.slatedForDeletetion || this.room != rCam.room)
			{
				sLeaser.CleanSpritesAndRemove();
				return;
			}
			TriangleMesh triangleMesh = sLeaser.sprites[0] as TriangleMesh;
			Vector2 vector = this.startPoint;
			Vector2 vector2 = this.stuckAt;
			float d = 2f * (this.lifetime / this.maxLifetime);
			for (int i = 0; i < 10; i++)
			{
				float f = (float)i / 9f;
				Vector2 a = Custom.DirVec(vector, this.endPoint);
				float num = Vector2.Distance(vector, this.endPoint);
				Vector2 vector3 = Custom.Bezier(this.stuckAt, this.handle, vector + a * num, vector + a * (num + 5f), f);
				Vector2 vector4 = Custom.DirVec(vector2, vector3);
				Vector2 a2 = Custom.PerpendicularVector(vector4);
				float d2 = Vector2.Distance(vector2, vector3);
				triangleMesh.MoveVertice(i * 4, vector3 - vector4 * d2 * 0.3f - a2 * d - camPos);
				triangleMesh.MoveVertice(i * 4 + 1, vector3 - vector4 * d2 * 0.3f + a2 * d - camPos);
				triangleMesh.MoveVertice(i * 4 + 2, vector3 - a2 * d - camPos);
				triangleMesh.MoveVertice(i * 4 + 3, vector3 + a2 * d - camPos);
				vector2 = vector3;
			}
			triangleMesh.color = Color.Lerp(Color.white, new Color(0.19607843f, 0f, 0.19607843f, 1f), 1f - this.lifetime / this.maxLifetime);
		}

		public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
		}

		public Vector2 endPoint;

		public Vector2 startPoint;

		private Vector2 stuckAt;

		private Vector2 handle;

		public float maxLifetime;

		public float lifetime;
	}

}