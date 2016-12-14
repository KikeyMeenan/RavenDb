using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raven.Client.Indexes;

namespace RavenDb
{
    public class Characters_ByName : AbstractIndexCreationTask<Character>
    {
        public Characters_ByName()
        {
            Map = characters => characters.Select(character => new {character.Name});

        }
    }
}
