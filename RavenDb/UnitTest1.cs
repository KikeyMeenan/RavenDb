using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Raven.Client.Document;
using Raven.Client.Indexes;
using Raven.Client.Linq;

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
            IndexCreation.CreateIndexes(typeof(Characters_ByName).Assembly, documentStore);
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
    }
}
