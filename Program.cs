using System;
using System.Text.Json;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace HNTitles
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var client = new HttpClient();

            var currentNews = await client.GetStreamAsync("https://hacker-news.firebaseio.com/v0/topstories.json");
            var currentItemIds = await JsonSerializer.DeserializeAsync<List<int>>(currentNews);

            Console.WriteLine($"Processing {currentItemIds.Count} news items");

            var webTasks = new List<Task<Item>>();
            foreach (var itemId in currentItemIds) {
                webTasks.Add(LoadItemFromWeb(itemId, client));
            }
            var itemsFromWeb = await Task.WhenAll(webTasks);

            var changeResults = new List<ChangeResult>();
            using (var db = new ItemContext())
            {
                foreach (var item in itemsFromWeb) {
                    changeResults.Add(await UpdateItemIfChanged(item, db));
                }
            }

            var resultsByType = changeResults.ToLookup(cr => cr.ChangeType);
            Console.WriteLine($"{resultsByType[ChangeType.Unchanged].Count()} items processed and unchanged");
            Console.WriteLine($"{resultsByType[ChangeType.New].Count()} new items: {string.Join(", ", resultsByType[ChangeType.New].Select(cr => cr.ItemEntry.ItemId))}");
            Console.WriteLine($"{resultsByType[ChangeType.Changed].Count()} items changed: {string.Join(", ", resultsByType[ChangeType.Changed].Select(cr => cr.ItemEntry.ItemId))}");
        }

        private static async Task<Item> LoadItemFromWeb(int itemId, HttpClient client) {
            var itemStream = await client.GetStreamAsync($"https://hacker-news.firebaseio.com/v0/item/{itemId}.json");
            var item = await JsonSerializer.DeserializeAsync<Item>(itemStream);

            Console.WriteLine($"Found item with title \"{item.Title}\"");
            return item;
        }

        private static async Task<ChangeResult> UpdateItemIfChanged(Item item, ItemContext db) {
            Console.Write($"Processing {item.ItemId}... ");
            

            ItemEntry previousEntry = null;

            var lastEntryForItem = await db.ItemEntries
                .Include(entry => entry.Item)
                .Where(entry => entry.ItemId == item.ItemId)
                .OrderBy(entry => entry.RecordedAt)
                .LastOrDefaultAsync();

            if (lastEntryForItem != null) {


                if (!lastEntryForItem.Item.Equals(item)) {
                    previousEntry = lastEntryForItem;
                    Console.WriteLine("Found a new entry!");
                    Console.WriteLine($"{previousEntry.Item.Title} -> {item.Title}");
                    Console.WriteLine($"{previousEntry.Item.URL} -> {item.URL}");
                    
                    var newEntry = new ItemEntry() {
                        Item = item,
                        PreviousEntry = previousEntry,
                        RecordedAt = DateTimeOffset.Now.ToUnixTimeMilliseconds()
                    };

                    await db.ItemEntries.AddAsync(newEntry);
                    await db.SaveChangesAsync();

                    return new ChangeResult(ChangeType.Changed, newEntry);
                }
                else {
                    Console.WriteLine($"No updates for item {item.ItemId}");
                    return new ChangeResult(ChangeType.Unchanged, lastEntryForItem);
                }

            }
            else {
                Console.WriteLine($"This is a new entry, saving {item.ItemId} : {item.Title}");
                var newEntry = new ItemEntry() {
                    Item = item,
                    RecordedAt = DateTimeOffset.Now.ToUnixTimeMilliseconds()
                };

                db.ItemEntries.Add(newEntry);
                await db.SaveChangesAsync();
                return new ChangeResult(ChangeType.New, newEntry);
            }
        }
    }

    class ChangeResult {
        public ChangeType ChangeType { get; set; }
        public ItemEntry ItemEntry { get; set; }

        public ChangeResult(ChangeType changeType, ItemEntry itemEntry)
        {
            ChangeType = changeType;
            ItemEntry = itemEntry;
        }
    }

    enum ChangeType {
        New,
        Changed,
        Unchanged
    }

    public class Item {
        [JsonPropertyName("id")]
        public int ItemId { get; set; }
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("url")]
        public Uri URL { get; set; }


        public override bool Equals(object other) {
            return Equal(other as Item);
        }

        private bool Equal(Item other) {
            if (other == null) return false;

            return other.ItemId == ItemId &&
                   other.Title == Title &&
                   other.URL == URL;
        }

        public override int GetHashCode() {
            return HashCode.Combine(ItemId, Title, URL);
        }
    }

    public class ItemEntry {
        public int ItemEntryId { get; set; }
        public int ItemId { get; set; }
        public Item Item { get; set; }
        public long RecordedAt { get; set; }
        public int PreviousEntryItemId { get; set; }
        public ItemEntry PreviousEntry { get; set; }
    }

    public class ItemContext : DbContext
    {
        public DbSet<Item> Items { get; set; }
        public DbSet<ItemEntry> ItemEntries { get; set; }

        // The following configures EF to create a Sqlite database file as `C:\blogging.db`.
        // For Mac or Linux, change this to `/tmp/blogging.db` or any other absolute path.
        // TODO construct an absolute path that will work on any platform
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite(@"Data Source=E:\hnitems.db");
    }
}
