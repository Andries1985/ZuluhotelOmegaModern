using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
    public class SBLeatherWorker : SBInfo
    {
        private List<GenericBuyInfo> m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBLeatherWorker()
        {
        }

        public override IShopSellInfo SellInfo
        {
            get { return m_SellInfo; }
        }

        public override List<GenericBuyInfo> BuyInfo
        {
            get { return m_BuyInfo; }
        }

        public class InternalBuyInfo : List<GenericBuyInfo>
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(Hide), 4, 999, 0x1078, 0));
                Add(new GenericBuyInfo(typeof(ThighBoots), 56, 10, 0x1711, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                Add(typeof(Hide), 1);
                Add(typeof(ThighBoots), 2);
            }
        }
    }
}