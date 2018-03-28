using Microsoft.DirectX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.SceneLoader;

namespace TGC.Group.Model
{
    class XwingShips
    {
        private List<TgcMesh> xwing = new List<TgcMesh>();

        public XwingShips(string path, Vector3 position)
        {
            xwing = new TgcSceneLoader().loadSceneFromFile(path).Meshes;
            
            foreach (var part in xwing)
            {
                part.Position = part.Position + positionhay;
            }
        }

        public void Render()
        {
            foreach(var part in xwing)
            {
                part.render();
            }
        }
    }
}
