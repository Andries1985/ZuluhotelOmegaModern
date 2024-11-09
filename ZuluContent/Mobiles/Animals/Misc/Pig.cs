namespace Server.Mobiles
{
    [CorpseName( "a pig corpse" )]
	public class Pig : BaseCreature
	{

		[Constructible]
public Pig() : base( AIType.AI_Animal, FightMode.Aggressor, 10, 1, 0.2, 0.4 )
		{
			Name = "a pig";
			Body = 0xCB;
			BaseSoundID = 0xC4;

			SetStr( 20 );
			SetDex( 20 );
			SetInt( 5 );

			SetHits( 12 );
			SetMana( 0 );

			SetDamage( 2, 4 );

			SetSkill( SkillName.MagicResist, 5.0 );
			SetSkill( SkillName.Tactics, 5.0 );
			SetSkill( SkillName.Wrestling, 5.0 );

			Fame = 150;
			Karma = 0;

			VirtualArmor = 12;

			Tamable = true;
			ControlSlots = 1;
			MinTameSkill = 11.1;
		}

		public override int Meat{ get{ return 1; } }
		public override FoodType FavoriteFood{ get{ return FoodType.FruitsAndVegies | FoodType.GrainsAndHay; } }

		[Constructible]
public Pig(Serial serial) : base(serial)
		{
		}

		public override void Serialize(IGenericWriter writer)
		{
			base.Serialize(writer);

			writer.Write((int) 0);
		}

		public override void Deserialize(IGenericReader reader)
		{
			base.Deserialize(reader);

			int version = reader.ReadInt();
		}
	}
}
