namespace Server.Items
{
    public class Citrine : Item
	{
		public override double DefaultWeight
		{
			get { return 0.1; }
		}


		[Constructible]
public Citrine() : this( 1 )
		{
		}


		[Constructible]
public Citrine( int amount ) : base( 0xF15 )
		{
			Stackable = true;
			Amount = amount;
		}

		[Constructible]
public Citrine( Serial serial ) : base( serial )
		{
		}



		public override void Serialize( IGenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( IGenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}
