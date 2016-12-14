using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Indexes;
using Raven.Client.Linq;
using Raven.Json.Linq;

namespace RavenDb
{
    [TestClass]
    public class UnitTest1
    {
        private DocumentStore documentStore;

        [TestInitialize]
        public void Initialize()
        {
            documentStore = new DocumentStore() { Url = "http://hvrjvz1:8080/" };
            documentStore.Initialize();
        }

        [TestCleanup]
        public void CleanUp()
        {
            documentStore.Dispose();
        }

        [TestMethod]
        public void Saving()
        {
            using (var session = documentStore.OpenSession())
            {
                #region character
                var character = new Character()
                {
                    Name = "Mike Keenan",
                    Class = new CharacterClass()
                    {
                        Name = "Developer"
                    },
                    Race = new Race()
                    {
                        Name = "Robot"
                    },
                    Inverntory = new List<Item>()
                    {
                        new Item()
                        {
                            Attack = 50,
                            Defence = 0,
                            Name = "Flamethrower"
                        }
                    }
                };
                #endregion

                session.Store(character);

                session.SaveChanges();
            }
        }

        [TestMethod]
        public void Loading()
        {
            using (var session = documentStore.OpenSession())
            {
                var character = session.Load<Character>("Characters/1");
                Console.WriteLine(character.Name);
                Console.WriteLine(character.Class.Name);
            }
        }

        [TestMethod]
        public void Updating()
        {
            using (var session = documentStore.OpenSession())
            {
                var character = session.Load<Character>("Characters/1");
                character.Name = "Jim";
                character.Class.Name = "Badass";
                session.SaveChanges();
            }
        }

        [TestMethod]
        public void Delete()
        {
            using (var session = documentStore.OpenSession())
            {
                var character = session.Load<Character>("Characters/65");
                session.Delete(character);
                session.SaveChanges();
            }
        }

        [TestMethod]
        public void Query()
        {
            using (var session = documentStore.OpenSession())
            {
                var characters = session.Query<Character>().Where(x => x.Inverntory.Any(i=>i.Attack > 5));
                foreach (var character in characters)
                {
                    Console.WriteLine(character.Name);
                }
            }
        }

        [TestMethod]
        public void Query_With_Index()
        {
            using (var session = documentStore.OpenSession())
            {
                var characters = session.Query<Character, Characters_ByName>().Where(x=>x.Name.StartsWith("Mike"));
                foreach (var character in characters)
                {
                    Console.WriteLine(character.Name);
                }
            }
        }

        [TestMethod]
        public void Query_With_Index_Advanced()
        {
            using (var session = documentStore.OpenSession())
            {
                var result = session.Query<Characters_CharacterCountByClass.ReduceResult, Characters_CharacterCountByClass>()
                    .FirstOrDefault(x=>x.ClassName == "Some Class") ?? new Characters_CharacterCountByClass.ReduceResult();

                Console.WriteLine(string.Format("{0} : {1}", result.ClassName, result.Count));
            }
        }

        [TestMethod]
        public void Store_Attachment()
        {
            var file = File.OpenRead(@"c:\temp\testimg.jpg");

            documentStore.DatabaseCommands.PutAttachment("images/1", null, file, new RavenJObject(){{"Dexcription", "An Image" }});

            using (var session = documentStore.OpenSession())
            {
                #region character
                var character = new Character()
                {
                    Name = "Mike Keenan",
                    Class = new CharacterClass()
                    {
                        Name = "Developer"
                    },
                    Race = new Race()
                    {
                        Name = "Robot"
                    },
                    Inverntory = new List<Item>()
                    {
                        new Item()
                        {
                            Attack = 50,
                            Defence = 0,
                            Name = "Flamethrower"
                        }
                    },
                    ImageId = "images/1"
                };
                #endregion

                session.Store(character);

                session.SaveChanges();
            }
        }

        [TestMethod]
        public void GetAttachment()
        {
            Character character = null;

            using (var session = documentStore.OpenSession())
            {
                character = session.Load<Character>("Characters/161");
            }

            var attachment = documentStore.DatabaseCommands.GetAttachment(character.ImageId);
            Console.WriteLine("Size: {0}", attachment.Size);
        }

        [TestMethod]
        public void Paging()
        {
            using (var session = documentStore.OpenSession())
            {
                RavenQueryStatistics stats;

                int pageNumber = 0;
                int resultsPerPage = 2;
                
                var characters = session.Query<Character>()
                    .Statistics(out stats)
                    .Customize(x=>x.Include<Character>(i=>i.SiblingId))
                    .Skip(pageNumber * resultsPerPage)
                    .Take(resultsPerPage)
                    .ToArray();

                foreach (var character in characters)
                {
                    Console.WriteLine(character.Name);
                }

                Console.WriteLine("Total Characters : " + stats.TotalResults);
                Console.WriteLine("Index Stale? " + stats.IsStale);
            }
        }

        [TestMethod]
        public void Include()
        {
            using (var session = documentStore.OpenSession())
            {
                #region character
                var character = new Character()
                {
                    Name = "User 2",
                    Class = new CharacterClass()
                    {
                        Name = "Samurai"
                    },
                    Race = new Race()
                    {
                        Name = "Alien"
                    },
                    Inverntory = new List<Item>()
                    {
                        new Item()
                        {
                            Attack = 8,
                            Defence = 0,
                            Name = "Throwing Knives"
                        }
                    }
                };
                #endregion

                session.Store(character);

                #region character2
                var character2 = new Character()
                {
                    Name = "User 2 Sibling",
                    Class = new CharacterClass()
                    {
                        Name = "Ninja"
                    },
                    Race = new Race()
                    {
                        Name = "Alien"
                    },
                    Inverntory = new List<Item>()
                    {
                        new Item()
                        {
                            Attack = 1000,
                            Defence = 0,
                            Name = "RPG"
                        }
                    },
                    SiblingId = character.Id
                };
                #endregion

                session.Store(character2);

                session.SaveChanges();
            }
        }

        [TestMethod]
        public void Test_Loading_With_Include()
        {
            using (var session = documentStore.OpenSession())
            {
                var character = session.Include<Character>(x=>x.SiblingId).Load("Characters/194");
                Console.WriteLine(character.Name);
                Console.WriteLine(character.Class.Name);

                var character2 = session.Load<Character>(character.SiblingId);
                Console.WriteLine(character2.Name);
                Console.WriteLine(character2.Class.Name);
            }
        }

        [TestMethod]
        public void Patching()
        {
            var newItem = new Item()
            {
                Name = "Old Boot",
                Attack = 1,
                Defence = 1
            };

            documentStore.DatabaseCommands.Patch("Characters/194", new[]
            {
                new PatchRequest()
                {
                    Type = PatchCommandType.Add,
                    Name = "Inverntory",
                    Value = RavenJObject.FromObject(newItem)
                }
            });
        }
    }
}
