namespace XPRising.Models.RandomEncounters
{
    public class ItemDataModel
    {
        public ItemDataModel(string line)
        {
            var fields = line.Split("\t");
            if (fields.Length != 5)
            {
                return;
            }

            Id = int.Parse(fields[0]);
            Name = fields[1];
            PrefabName = fields[2];
            Rarity = int.Parse(fields[3]);
            Obtainable = bool.Parse(fields[4]);
        }
        public int Id { get; set; }
        public string Name { get; set; }
        public string PrefabName { get; set; }
        public int Rarity { get; set; }
        public bool Obtainable { get; set; }

        public string Color =>
            Rarity switch
            {
                0 => "#ffffff",
                1 => "#10e000",
                2 => "#008bff",
                3 => "#ff00e9",
                4 => "#ff8400",
                _ => "#ffffff"
            };
    }
}