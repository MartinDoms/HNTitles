using System;
using System.Text.Json;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;
using System.IO;

namespace HNTitles
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var client = new HttpClient();

            Dictionary<string, ItemEntry> loadedEntries = await JsonSerializer.DeserializeAsync<Dictionary<string, ItemEntry>>(
                File.OpenRead("data.json")
            );
            var allEntries = new ConcurrentDictionary<string, ItemEntry>(loadedEntries);

            var currentNews = await client.GetStreamAsync("https://hacker-news.firebaseio.com/v0/topstories.json");
            var currentItemIds = await JsonSerializer.DeserializeAsync<List<int>>(currentNews);

            Console.WriteLine($"Processing {currentItemIds.Count} news items");

            List<Task> tasks = new List<Task>();
            foreach (var itemId in currentItemIds) {
                tasks.Add(LoadAndUpdate(itemId, allEntries, client));
            }
            
            await Task.WhenAll(tasks.ToArray());
            await File.WriteAllTextAsync("data.json", JsonSerializer.Serialize(allEntries));
        }

        private static async Task LoadAndUpdate(int itemId, ConcurrentDictionary<string, ItemEntry> allEntries, HttpClient client) {
            Console.WriteLine($"Processing {itemId}");
            var itemString = itemId.ToString();
            var itemStream = await client.GetStreamAsync($"https://hacker-news.firebaseio.com/v0/item/{itemId}.json");
            var item = await JsonSerializer.DeserializeAsync<Item>(itemStream);

            Console.WriteLine($"Found item with title \"{item.Title}\"");

            ItemEntry previousEntry = null;

            if (allEntries.ContainsKey(itemString)) {

                if (!allEntries[itemString].Item.Equals(item)) {
                    previousEntry = allEntries[itemString];
                    Console.WriteLine("Found a new entry!");
                    Console.WriteLine($"{previousEntry.Item.Title} -> {item.Title}");
                    Console.WriteLine($"{previousEntry.Item.URL} -> {item.URL}");
                    
                    var newEntry = new ItemEntry() {
                        Item = item,
                        PreviousEntry = previousEntry,
                        RecordedAt = DateTimeOffset.Now
                    };

                    allEntries[itemString] = newEntry;
                }

            }
            else {
                var newEntry = new ItemEntry() {
                    Item = item,
                    RecordedAt = DateTimeOffset.Now
                };

                allEntries[itemString] = newEntry;
            }
        }
    }

    public class Item {
        [JsonPropertyName("id")]
        public int ID { get; set; }
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("url")]
        public Uri URL { get; set; }

        public override bool Equals(object other) {
            return Equal(other as Item);
        }

        private bool Equal(Item other) {
            if (other == null) return false;

            return other.ID == ID &&
                   other.Title == Title &&
                   other.URL == URL;
        }

        public override int GetHashCode() {
            return HashCode.Combine(ID, Title, URL);
        }
    }

    public class ItemEntry {
        public Item Item { get; set; }
        public DateTimeOffset RecordedAt { get; set; }
        public ItemEntry PreviousEntry { get; set; }
    }
}
