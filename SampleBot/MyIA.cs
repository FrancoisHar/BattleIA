using System;
using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;
using BattleIA;
using System.Collections;
using System.Collections.Generic;


namespace SampleBot {

     static int minDistance(int[] distance, bool[] test, int scanMap)
    {
        int min = int.MaxValue;
        int minIndex = 0;

        for (int v = 0; v < scanMap; ++v)
        {
            if (test[v] == false && distance[v] <= min)
            {
                min = distance[v];
                minIndex = v;
            }
        }

        return minIndex;
    }

    private static void Print (int[] distance, int verticesCount)
    {
        Console.WriteLine("Vertex    Distance from source");

        for (int i = 0; i < verticesCount; ++i)
            Console.WriteLine("{0}\t  {1}", i, distance[i]);
    }

    public static void Dijkstra(int[,] graph, int source, int verticesCount)
    {
        int[] distance = new int[verticesCount];
        bool[] shortestPathTreeSet = new bool[verticesCount];

        for (int i = 0; i < verticesCount; ++i)
        {
            distance[i] = int.MaxValue;
            shortestPathTreeSet[i] = false;
        }

        distance[source] = 0;

        for (int count = 0; count < verticesCount - 1; ++count)
        {
            int u = minDistance(distance, shortestPathTreeSet, verticesCount);
            shortestPathTreeSet[u] = true;

            for (int v = 0; v < verticesCount; ++v)
                if (!shortestPathTreeSet[v] && Convert.ToBoolean(graph[u, v]) && distance[u] != int.MaxValue && distance[u] + graph[u, v] < distance[v])
                    distance[v] = distance[u] + graph[u, v];
        }

        Console.WriteLine(distance, verticesCount);
    }


    class NRJPOINT
    {
        public int posx;
        public int posy;
        public int distance;

        public int get_distance(int x, int y)
        {
            distance = Math.Abs(x - posx) + Math.Abs(y - posy);
            return distance;
        }
    }

    public class MyIA
    {

        Random rnd = new Random();
        bool isFirst = true;
        UInt16 currentShieldLevel = 0;
        bool hasBeenHit = false;
        //my relative position in current scan.
        int meY = 0;
        int meX = 0;

        List<MoveDirection> route = new List<MoveDirection>();


        public MyIA()

        {
            // setup
        }

        /// <summary>
        /// Mise à jour des informations
        /// </summary>
        /// <param name="turn">Turn.</param>
        /// <param name="energy">Energy.</param>
        /// <param name="shieldLevel">Shield level.</param>
        /// <param name="isCloaked">If set to <c>true</c> is cloacked.</param>
        public void StatusReport(UInt16 turn, UInt16 energy, UInt16 shieldLevel, bool isCloaked)
        {
            // si le niveau de notre bouclier a baissé, c'est que l'on a reçu un coup
            if (currentShieldLevel != shieldLevel)
            {
                currentShieldLevel = shieldLevel;
                hasBeenHit = true;
            }
        }


        /// <summary>
        /// On nous demande la distance de scan que l'on veut effectuer
        /// </summary>
        /// <returns>The scan surface.</returns>
        public byte GetScanSurface()
        {
            if (isFirst)
            {
                isFirst = false;
                return 10;
            }
            if (route.Count == 0)
            {
                return 10;
            }
            return 0;
        }

        List<MoveDirection> find_nearest_energy(List<NRJPOINT> pliste)
        {
            // Let's compute distance & find the nearest Energy point 
            // pour chaque nrjtpoint appelé 'p' de nrj_list : 
            // distance = valeur absolue (meX - nrjpoint.posx)+ abs(meY-nrjpoint.posy)
            NRJPOINT target = new NRJPOINT();
            target.distance = 9999;
            foreach (NRJPOINT p in pliste)
            {
                p.get_distance(meX, meY);
                if (p.distance < target.distance)
                {
                    target = p;
                    target.posx = target.posx - meX;
                    target.posy = target.posy - meY;

                }
            }

            // lET'S generate a route : 
            // Est / west  : 
            if (target.posx < 0)
            {
                for (int i = 0; i < Math.Abs(target.posx); i++)
                {
                    route.Add(MoveDirection.East);
                }
            }
            if (target.posx > 0)
            {
                for (int i = 0; i < Math.Abs(target.posx); i++)
                {
                    route.Add(MoveDirection.West);
                }
            }
            if (target.posy < 0)
            {
                for (int i = 0; i < Math.Abs(target.posy); i++)
                {
                    route.Add(MoveDirection.North);
                }
            }
            if (target.posy > 0)
            {
                for (int i = 0; i < Math.Abs(target.posy); i++)
                {
                    route.Add(MoveDirection.South);
                }
            }

            return route;
        }



        /// <summary>
        /// Résultat du scan
        /// </summary>
        /// <param name="distance">Distance.</param>
        /// <param name="informations">Informations.</param>
        public void AreaInformation(byte distance, byte[] informations)
        {
            if (distance == 0) { return; }

            int radar_nrj = 0;

            List<NRJPOINT> NRJ_list = new List<NRJPOINT>();

            Console.WriteLine($"Area: {distance}");
            int index = 0;
            for (int i = 0; i < distance; i++)
            {
                for (int j = 0; j < distance; j++)
                {
                    if (informations[index] == (byte)CaseState.Energy)
                    {
                        radar_nrj++;
                        NRJPOINT n = new NRJPOINT();
                        n.posx = j;
                        n.posy = i;
                        NRJ_list.Add(n);
                    }
                    if (informations[index] == (byte)CaseState.Ennemy)
                    {
                        meX = j;
                        meY = i;
                    }
                    index++;
                    //Console.Write(informations[index]);
                }
                //Console.WriteLine();
            }
            route = find_nearest_energy(NRJ_list);

        }

        //début modif
        public byte[] randommove()
        {
            byte[] ret; // ret = tab d'octet
            ret = new byte[2]; //tableau de taille 2 octet
            ret[0] = (byte)BotAction.Move;
            ret[1] = (byte)rnd.Next(1, 5);

            return ret;
        }
        //fin modif

        /// <summary>
        /// On dot effectuer une action
        /// </summary>
        /// <returns>The action.</returns>
        public byte[] GetAction()
        {
            byte[] ret;
            // nous venons d'être touché
            if (hasBeenHit)
            {
                // plus de bouclier ?
                if (currentShieldLevel == 0)
                {
                    // on en réactive 1 de suite !
                    currentShieldLevel = (byte)rnd.Next(1, 9);
                    ret = new byte[3];
                    ret[0] = (byte)BotAction.ShieldLevel;
                    ret[1] = (byte)(currentShieldLevel & 0xFF);
                    ret[2] = (byte)(currentShieldLevel >> 8);
                    return ret;
                }

                hasBeenHit = false;
                // puis on se déplace fissa, au hazard
                ret = new byte[2];
                ret[0] = (byte)BotAction.Move;
                ret[1] = (byte)rnd.Next(1, 5);
                return ret;
            }

            // si pas de bouclier, on en met un en route
            if (currentShieldLevel == 0)
            {
                // on en réactive 1 de suite !
                currentShieldLevel = 1;
                ret = new byte[3];
                ret[0] = (byte)BotAction.ShieldLevel;
                ret[1] = (byte)(currentShieldLevel & 0xFF);
                ret[2] = (byte)(currentShieldLevel >> 8);
                return ret;
            }

            ret = new byte[2];
            ret[0] = (byte)BotAction.Move;
            if (route.Count > 0)
            {
                ret[1] = (byte)route[0];
                route.RemoveAt(0);
            }
            else
            {
                // on se déplace au hazard
                ret[1] = (byte)rnd.Next(1, 5);
            }

            //var ret = new byte[1];
            //ret[0] = (byte)BotAction.None;

            //ret = new byte[2];
            //ret[0] = (byte)BotAction.Move;
            //ret[1] = (byte)MoveDirection.North;

            //var ret = new byte[2];
            //ret[0] = (byte)BotAction.ShieldLevel;
            //ret[1] = 10;

            //var ret = new byte[2];
            //ret[0] = (byte)BotAction.CloackLevel;
            //ret[1] = 20;

            // ret = new byte[2];
            // ret[0] = (byte)BotAction.ShieldLevel;
            // ret[1] = (byte)MoveDirection.North;

            return ret;
        }

    }
}
