namespace HobbyDS9.Contracts
{
    public sealed class PoliticalEconomySnapshot
    {
        public int CreditsFactor { get; }
        public int IndustryFactor { get; }
        public int ResearchFactor { get; }
        public int LogisticsFactor { get; }
        public int ExtractionFactor { get; }
        public bool AcceptsRefugees { get; }
        public PoliticalEconomySnapshot(int credits,int industry,int research,int logistics,int extraction,bool refugees)
        { CreditsFactor=credits;IndustryFactor=industry;ResearchFactor=research;LogisticsFactor=logistics;ExtractionFactor=extraction;AcceptsRefugees=refugees; }
    }
}
