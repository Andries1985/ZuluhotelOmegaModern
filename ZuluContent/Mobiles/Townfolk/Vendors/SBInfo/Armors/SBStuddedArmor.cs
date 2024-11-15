using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
    public class SBStuddedArmor: SBInfo
	{
		private List<GenericBuyInfo> m_BuyInfo = new InternalBuyInfo();
		private IShopSellInfo m_SellInfo = new InternalSellInfo();

		public SBStuddedArmor()
		{
		}

		public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
		public override List<GenericBuyInfo> BuyInfo { get { return m_BuyInfo; } }

		public class InternalBuyInfo : List<GenericBuyInfo>
		{
			public InternalBuyInfo()
			{
				Add( new GenericBuyInfo( typeof( StuddedArms ), 87, 20, 0x13DC, 0 ) );
				Add( new GenericBuyInfo( typeof( StuddedChest ), 128, 20, 0x13DB, 0 ) );
				Add( new GenericBuyInfo( typeof( StuddedGloves ), 79, 20, 0x13D5, 0 ) );
				Add( new GenericBuyInfo( typeof( StuddedGorget ), 73, 20, 0x13D6, 0 ) );
				Add( new GenericBuyInfo( typeof( StuddedLegs ), 103, 20, 0x13DA, 0 ) );
				Add( new GenericBuyInfo( typeof( FemaleStuddedChest ), 142, 20, 0x1C02, 0 ) );
				Add( new GenericBuyInfo( typeof( StuddedBustierArms ), 120, 20, 0x1c0c, 0 ) );
			}
		}

		public class InternalSellInfo : GenericSellInfo
		{
			public InternalSellInfo()
			{
				Add( typeof( StuddedArms ), 2 );
				Add( typeof( StuddedChest ), 2 );
				Add( typeof( StuddedGloves ), 2 );
				Add( typeof( StuddedGorget ), 2 );
				Add( typeof( StuddedLegs ), 2 );
				Add( typeof( FemaleStuddedChest ), 2 );
				Add( typeof( StuddedBustierArms ), 2 );
			}
		}
	}
}
