namespace Zoo
{
    public enum BatteryType
    {
        Unknown = 0,
        D = 1,
        //D batteries currently always come as 1 battery in the one battery holder
        AA = 2,
        //AA batteries currently always comes in 2 batteries in the one battery holder
    }

    public enum PortType
    {
        DVI,
        Parallel,
        PS2,
        RJ45,
        Serial,
        StereoRCA
    }

    public enum IndicatorLabel
    {
        SND,
        CLR,
        CAR,
        IND,
        FRQ,
        SIG,
        NSA,
        MSA,
        TRN,
        BOB,
        FRK,
        NLL
    }
}
