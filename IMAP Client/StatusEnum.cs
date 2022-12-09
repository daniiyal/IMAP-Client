namespace IMAP_Client
{
    public class Status
    {
        private Status(string value)
        {
            Value = value;
        }

        public string Value { get; private set; }


        public static Status OK => new Status("OK");
        public static Status NO => new("NO");

        public static Status BAD => new("BAD");

        public override string ToString()
        {
            return Value;
        }
    }
}

