using System;
using System.Text.Json;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using System.IO;

namespace HNTitles
{
    class Program
    {
        static async Task Main(string[] args)
        {
            bool reportOnly = args.Length > 0 && (args[0] == "--report" || args[0] == "-r");
            if (!reportOnly) {
                var client = new HttpClient();

                List<int> hnTopStoryIds = new List<int>();
                try {
                    var hnTopStories = await client.GetStreamAsync("https://hacker-news.firebaseio.com/v0/topstories.json");
                    hnTopStoryIds = await JsonSerializer.DeserializeAsync<List<int>>(hnTopStories);
                }
                catch (HttpRequestException requestException) {
                    Console.Error.WriteLine($"Problem fetching list of items: {requestException.StatusCode} {requestException.Message}");
                }
                
                Console.WriteLine($"Fetching {hnTopStoryIds.Count} news items");

                var webTasks = new List<Task<Item>>();
                foreach (var itemId in hnTopStoryIds) {
                    webTasks.Add(LoadItemFromWeb(itemId, client));
                }
                var itemsFromWeb = await Task.WhenAll(webTasks);
                var itemsToProcess = itemsFromWeb.Where(webItem => webItem is not null);

                Console.WriteLine($"\nProcessing {itemsToProcess.Count()} items");
                var changeResults = new List<ChangeResult>();
                using (var db = new ItemContext())
                {
                    foreach (var item in itemsToProcess) {
                        changeResults.Add(await UpdateItemIfChanged(item, db));
                        Console.Write(".");
                    }

                    Console.WriteLine();

                    var resultsByType = changeResults.ToLookup(cr => cr.ChangeType);
                    Console.WriteLine($"{resultsByType[ChangeType.Unchanged].Count()} items processed and unchanged");
                    Console.WriteLine($"{resultsByType[ChangeType.New].Count()} new items: {string.Join(", ", resultsByType[ChangeType.New].Select(cr => cr.Item.HnItemId))}");
                    Console.WriteLine($"{resultsByType[ChangeType.Changed].Count()} items changed: {string.Join(", ", resultsByType[ChangeType.Changed].Select(cr => cr.Item.HnItemId))}");

                }
            }
            using (var db = new ItemContext()) {
                var updated = db.Items.Where(i => i.PreviousItemId != null);
                foreach (var updatedItem in updated) {
                    Console.WriteLine($"\t{updatedItem.Title}");
                    Item currentItem = updatedItem;
                    while (currentItem.PreviousItemId != null) {
                        currentItem = db.Items.Single<Item>(i => i.ItemId == currentItem.PreviousItemId);
                        Console.WriteLine($"was\t{currentItem.Title}");
                    }
                    Console.WriteLine();
                }
            }
        }

        private static async Task<Item> LoadItemFromWeb(int itemId, HttpClient client) {
            try {
                var itemStream = await client.GetStreamAsync($"https://hacker-news.firebaseio.com/v0/item/{itemId}.json");
                var item = await JsonSerializer.DeserializeAsync<Item>(itemStream);

                Console.Write(".");

                return item;
            }
            catch (HttpRequestException requestException) {
                Console.Error.WriteLine($"Problem fetching item {itemId}: {requestException.StatusCode} {requestException.Message}");
                return null;
            }
        }

        private static async Task<ChangeResult> UpdateItemIfChanged(Item item, ItemContext db) {           
            var lastItemForId = await db.Items
                .Where(i => i.HnItemId == item.HnItemId)
                .OrderBy(entry => entry.RecordedAt)
                .LastOrDefaultAsync();

            if (lastItemForId != null) {
                if (!lastItemForId.Equals(item)) {
                    var previousItem = lastItemForId;
                    Console.WriteLine("\nFound an updated entry");
                    Console.WriteLine($"{previousItem.Title} -> {item.Title}");
                    Console.WriteLine($"{previousItem.URL} -> {item.URL}");
                    
                    var newEntry = item.WithPreviousItem(previousItem).WithRecordedDate(DateTimeOffset.Now);

                    await db.Items.AddAsync(newEntry);
                    await db.SaveChangesAsync();

                    return new ChangeResult(ChangeType.Changed, newEntry);
                }
                else {
                    return new ChangeResult(ChangeType.Unchanged, lastItemForId);
                }

            }
            else {
                Console.WriteLine($"\nNew entry, saving {item.HnItemId} : {item.Title}");

                var newItem = item.WithRecordedDate(DateTimeOffset.Now);
                db.Items.Add(newItem);
                await db.SaveChangesAsync();
                return new ChangeResult(ChangeType.New, newItem);
            }
        }
    }

    class ChangeResult {
        public ChangeType ChangeType { get; set; }
        public Item Item { get; set; }

        public ChangeResult(ChangeType changeType, Item item)
        {
            ChangeType = changeType;
            Item = item;
        }
    }

    enum ChangeType {
        New,
        Changed,
        Unchanged
    }

    public class Item {
        public Item(int hackernewsId, string title, Uri url ,long recordedAtUnixMillis, Item previousItem = null)
        {
            HnItemId = hackernewsId;
            Title = title;
            URL = url;
            RecordedAt = recordedAtUnixMillis;
            PreviousItem = previousItem;
        }

        public Item()
        {}

        public Item WithPreviousItem(Item previousItem) {
            return new Item(
                HnItemId,
                Title,
                URL,
                RecordedAt,
                previousItem
            );
        }

        public Item WithRecordedDate(DateTimeOffset dateTime) {
            return new Item(
                HnItemId,
                Title,
                URL,
                dateTime.ToUnixTimeMilliseconds(),
                PreviousItem
            );
        }

        public int ItemId { get; set; }
        [JsonPropertyName("id")]
        public int HnItemId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("url")]
        public Uri URL { get; set; }

        public long RecordedAt { get; set; }
        public int? PreviousItemId { get; set; }
        public Item PreviousItem { get; set; }


        public override bool Equals(object other) {
            return Equal(other as Item);
        }

        private bool Equal(Item other) {
            if (other == null) return false;

            return other.HnItemId == HnItemId &&
                   other.Title == Title &&
                   other.URL == URL;
        }

        public override int GetHashCode() {
            return HashCode.Combine(ItemId, Title, URL);
        }
    }
    public class ItemContext : DbContext
    {
        public DbSet<Item> Items { get; set; }

        private string DbPath { 
            get {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "hntitles.db"
                );
            }
        }

        // The following configures EF to create a Sqlite database file as `C:\blogging.db`.
        // For Mac or Linux, change this to `/tmp/blogging.db` or any other absolute path.
        // TODO construct an absolute path that will work on any platform
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={DbPath}");
    }
}
