using System;
using Server.Items;
using Server.Network;

namespace Server.Mobiles
{
    [CorpseName( "a sheep corpse" )]
	public class Sheep : BaseCreature, ICarvable
	{
		private DateTime m_NextWoolTime;

		[CommandProperty( AccessLevel.GameMaster )]
		public DateTime NextWoolTime
		{
			get{ return m_NextWoolTime; }
			set{ m_NextWoolTime = value; Body = DateTime.Now >= m_NextWoolTime ? 0xCF : 0xDF; }
		}

		public void Carve( Mobile from, Item item )
		{
			if ( DateTime.Now < m_NextWoolTime )
			{
				// This sheep is not yet ready to be shorn.
				PrivateOverheadMessage( MessageType.Regular, 0x3B2, 500449, from.NetState );
				return;
			}

			from.SendLocalizedMessage( 500452 ); // You place the gathered wool into your backpack.
			from.AddToBackpack( new Wool( Map == Map.Felucca ? 2 : 1 ) );

			NextWoolTime = DateTime.Now + TimeSpan.FromHours( 3.0 ); // TODO: Proper time delay
		}

		public override void OnThink()
		{
			base.OnThink();
			Body = DateTime.Now >= m_NextWoolTime ? 0xCF : 0xDF;
		}


		[Constructible]
public Sheep() : base( AIType.AI_Animal, FightMode.Aggressor, 10, 1, 0.2, 0.4 )
		{
			Name = "a sheep";
			Body = 0xCF;
			BaseSoundID = 0xD6;

			SetStr( 19 );
			SetDex( 25 );
			SetInt( 5 );

			SetHits( 12 );
			SetMana( 0 );

			SetDamage( 1, 2 );

			SetSkill( SkillName.MagicResist, 5.0 );
			SetSkill( SkillName.Tactics, 6.0 );
			SetSkill( SkillName.Wrestling, 5.0 );

			Fame = 300;
			Karma = 0;

			VirtualArmor = 6;

			Tamable = true;
			ControlSlots = 1;
			MinTameSkill = 11.1;
		}

		public override int Meat{ get{ return 3; } }
		public override MeatType MeatType{ get{ return MeatType.LambLeg; } }
		public override FoodType FavoriteFood{ get{ return FoodType.FruitsAndVegies | FoodType.GrainsAndHay; } }

		public override int Wool{ get{ return Body == 0xCF ? 3 : 0; } }

		[Constructible]
public Sheep( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( IGenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 1 );

			writer.WriteDeltaTime( m_NextWoolTime );
		}

		public override void Deserialize( IGenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 1:
				{
					NextWoolTime = reader.ReadDeltaTime();
					break;
				}
			}
		}
	}
}
