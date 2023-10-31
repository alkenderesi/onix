using SnakeLib;
using SnakeLib.Brains;
using System;
using System.Collections.Generic;

namespace Onix
{
    public class Onix : SnakeBrainBase
    {
        public override string Name => "Onix";

        public override string Author => "Kenderesi Antal";

        public override Direction GetNextMovement(bool grow)
        {
            // 'depth' is the most important variable for this brain
            // In a nutshell it determines how many steps the snake thinks ahead
            // By increasing its value the snake will become smarter but also much slower

            int depth = 2;
            
            CustomSnake mySnake = new CustomSnake(Me, Field);
            CustomSnake enemySnake = new CustomSnake(Enemy, Field);
            CustomAppleList appleList = new CustomAppleList(Field);

            List<List<int>> myDistance = GetDistances(mySnake, enemySnake, appleList, int.MaxValue);
            List<List<int>> enemyDistance = GetDistances(enemySnake, mySnake, appleList, int.MaxValue);

            return (Direction)GetBestStep(mySnake, enemySnake, appleList, depth * 2, depth * 2, true, int.MinValue, int.MaxValue);
        }

        /// <summary>
        /// Recursively calculates the best optimal step based on possible future fields, featuring minimax algorithm with alpha-beta pruning
        /// </summary>
        private int GetBestStep(CustomSnake mySnake, CustomSnake enemySnake, CustomAppleList appleList,
                            int originalDepth, int currentDepth, bool myTurn, int alpha, int beta)
        {
            // Evaluating whether someone won
            if (currentDepth % 2 == 0)
            {
                int winEval = EvaluateWinner(mySnake, enemySnake, originalDepth - currentDepth + 1, currentDepth);
                if (winEval != 0)
                {
                    return winEval;
                }
            }
            // Evaluating field when we reached the bottom of the search tree
            if (currentDepth == 0)
            {
                return EvaluateField(mySnake, enemySnake, appleList);
            }
            if (myTurn)
            {
                // Preparation for simulating a step
                appleList.UpdateLifes();
                int bestValue = int.MinValue;
                int bestDirection = 0;
                bool flag = originalDepth == currentDepth;

                // Adding randomness to the snakes movement
                Random rnd = new Random();
                int baseDirection;
                if(rnd.Next(1,10) == 1)
                {
                    baseDirection = rnd.Next(0,3);
                }
                else
                {
                    baseDirection = mySnake.LastDirection;
                }
                
                // Simulating all 4 possible steps
                for (int i = 0; i < 4; i++)
                {
                    int currentDirection = (baseDirection + i) % 4;

                    // Pruning instant death steps
                    if (IsInstantDeath(currentDirection, mySnake.LastDirection))
                    {
                        continue;
                    }

                    // Copying nessessery data
                    CustomSnake stepMySnake = new CustomSnake(mySnake);
                    CustomAppleList stepAppleList = new CustomAppleList(appleList);

                    // Performing step
                    stepMySnake.Move(currentDirection, Field);
                    stepMySnake.ConsumeApples(stepAppleList, Field.PointsForApple, currentDepth);

                    // Recursion time
                    int stepValue = GetBestStep(stepMySnake, enemySnake, stepAppleList, originalDepth, currentDepth - 1, false, alpha, beta);

                    // Comparing results with the current best available step
                    if (stepValue > bestValue)
                    {
                        bestValue = stepValue;
                        if (flag)
                        {
                            bestDirection = currentDirection;
                        }
                    }

                    // Alpha-Beta Pruning
                    alpha = Math.Max(alpha, bestValue);
                    if (alpha >= beta)
                    {
                        break;
                    }
                }
                // Returning the final results
                if (flag)
                {
                    return bestDirection;
                }
                return bestValue;
            }
            else
            {
                // Preparation for simulating a step
                int bestValue = int.MaxValue;

                // Simulating all 4 possible steps
                for (int i = 0; i < 4; i++)
                {
                    int currentDirection = (enemySnake.LastDirection + i) % 4;

                    // Pruning instant death steps
                    if (IsInstantDeath(currentDirection, enemySnake.LastDirection))
                    {
                        continue;
                    }

                    // Copying nessessery data
                    CustomSnake stepEnemySnake = new CustomSnake(enemySnake);
                    CustomAppleList stepAppleList = new CustomAppleList(appleList);

                    // Performing step
                    stepEnemySnake.Move(currentDirection, Field);
                    stepEnemySnake.ConsumeApples(stepAppleList, Field.PointsForApple, currentDepth + 1);

                    // Recursion time
                    int stepValue = GetBestStep(mySnake, stepEnemySnake, stepAppleList, originalDepth, currentDepth - 1, true, alpha, beta);

                    // Comparing results with the current best available step
                    if (stepValue < bestValue)
                    {
                        bestValue = stepValue;
                    }

                    // Alpha-Beta Pruning
                    beta = Math.Min(beta, bestValue);
                    if (beta <= alpha)
                    {
                        break;
                    }
                }

                // Returning the final results
                return bestValue;
            }
        }

        /// <summary>
        /// The value of the field when there is a winner
        /// </summary>
        private int EvaluateWinner(CustomSnake mySnake, CustomSnake enemySnake, int depthReward, int depthPunish)
        {
            int win = int.MaxValue - depthPunish;
            int wallhit = int.MinValue + depthReward;
            int selfhit = int.MinValue + depthReward * 2;
            int opphit = int.MinValue + depthReward * 3;
            int pointdef = int.MinValue + depthReward * 4;
            int tie = int.MinValue + depthReward * 5;
            bool flag1 = IsHeadInWall(mySnake);
            bool flag2 = IsHeadInWall(enemySnake);
            bool flag3 = IsHeadInSelf(mySnake);
            bool flag4 = IsHeadInSelf(enemySnake);
            bool flag5 = IsHeadInOpponent(mySnake, enemySnake);
            bool flag6 = IsHeadInOpponent(enemySnake, mySnake);
            bool flag7 = enemySnake.AppleScore == Field.WinPoints;
            bool flag8 = mySnake.AppleScore == Field.WinPoints;
            bool myDefeat = flag1 || flag3 || flag5 || flag7;
            bool enemyDefeat = flag2 || flag4 || flag6 || flag8;
            if(myDefeat)
            {
                if(enemyDefeat && mySnake.AppleScore > enemySnake.AppleScore)
                {
                    return win;
                }
                if(enemyDefeat && mySnake.AppleScore == enemySnake.AppleScore)
                {
                    return tie;
                }
                if(flag1)
                {
                    return wallhit;
                }
                if(flag3)
                {
                    return selfhit;
                }
                if(flag5)
                {
                    return opphit;
                }
                if(flag7)
                {
                    return pointdef;
                }
            }
            if(enemyDefeat)
            {
                return win;
            }
            return 0;
        }

        /// <summary>
        /// The value of the field
        /// </summary>
        private int EvaluateField(CustomSnake mySnake, CustomSnake enemySnake, CustomAppleList appleList)
        {
            ulong myOldReachableCount = mySnake.ReachableCount;
            ulong enemyOldReachableCount = enemySnake.ReachableCount;
            int myOldTailDistance = mySnake.TailDistance;
            int enemyOldTailDistance = enemySnake.TailDistance;

            List<List<int>> myDistance = GetDistances(mySnake, enemySnake, appleList, (int)(myOldReachableCount / 5));
            List<List<int>> enemyDistance = GetDistances(enemySnake, mySnake, appleList, (int)(enemyOldReachableCount / 5));

            mySnake.PlayScore += AppleDistanceReward(appleList, myDistance);
            enemySnake.PlayScore += AppleDistanceReward(appleList, enemyDistance);

            mySnake.PlayScore += TailDistanceDecreasePunish((ulong)mySnake.TailDistance, (ulong)myOldTailDistance);
            enemySnake.PlayScore += TailDistanceDecreasePunish((ulong)enemySnake.TailDistance, (ulong)enemyOldTailDistance);

            mySnake.PlayScore += ReachableDecreasePunish(mySnake.ReachableCount, myOldReachableCount);
            enemySnake.PlayScore += ReachableDecreasePunish(enemySnake.ReachableCount, enemyOldReachableCount);

            return mySnake.PlayScore - enemySnake.PlayScore;
        }

        /// <summary>
        /// Calculates the shortest distances for all necessary locations, basically a snake optimized version of Dijkstra's algorithm
        /// </summary>
        private List<List<int>> GetDistances(CustomSnake checkSnake, CustomSnake otherSnake, CustomAppleList appleList, int limit)
        {
            List<List<int>> distance = GetDistanceDefault();
            List<List<bool>> visited = GetVisitedDefault();
            CustomLocation visitLocation = new CustomLocation(checkSnake.Head);
            distance[visitLocation.X][visitLocation.Y] = 0;
            visited[visitLocation.X][visitLocation.Y] = true;
            ulong visitCount = 0;
            while (ContinueCalculation(checkSnake, visitLocation, visited, distance, appleList, visitCount, limit))
            {
                UpdateAdjacentDistances(checkSnake, otherSnake, visitLocation, visited, distance);
                visitCount++;
                visitLocation = UpdateNextVisitLocation(checkSnake, visited, distance);
            }
            UpdateTailDistance(checkSnake, distance);
            checkSnake.ReachableCount = visitCount - 1;
            checkSnake.TailDistance = distance[checkSnake.Tail.X][checkSnake.Tail.Y];
            return distance;
        }

        /// <summary>
        /// Punish score for drastically decreased reachable count
        /// </summary>
        private int ReachableDecreasePunish(ulong currentReachableCount, ulong oldReachableCount)
        {
            if (currentReachableCount < oldReachableCount / 5)
            {
                return -Field.WinPoints;
            }
            return 0;
        }

        /// <summary>
        /// Punish score for drastically decreased tail distance
        /// </summary>
        private int TailDistanceDecreasePunish(ulong currentTailDistance, ulong oldTailDistance)
        {
            if (currentTailDistance > oldTailDistance * 10)
            {
                return -1;
            }
            return 0;
        }

        /// <summary>
        /// Score based on the distance from apples
        /// </summary>
        private int AppleDistanceReward(CustomAppleList appleList, List<List<int>> distance)
        {
            if (appleList.IsEmpty())
            {
                return 0;
            }
            int min1 = int.MaxValue;
            int min2 = int.MaxValue;
            for (int i = 0; i < appleList.Length; i++)
            {
                if (appleList[i].LifeLeft - distance[appleList[i].X][appleList[i].Y] > 0 &&
                    distance[appleList[i].X][appleList[i].Y] < min1)
                {
                    min1 = distance[appleList[i].X][appleList[i].Y];
                }
                if (distance[appleList[i].X][appleList[i].Y] < min2)
                {
                    min2 = distance[appleList[i].X][appleList[i].Y];
                }
            }
            if (min1 != int.MaxValue)
            {
                return Field.PointsForApple - min1 - 1;
            }
            if (min2 != int.MaxValue)
            {
                return Field.PointsForApple - (min2 * 2) - 1;
            }
            return 0;
        }

        /// <summary>
        /// The location with the lowest distance which have not been visited yet
        /// </summary>
        private CustomLocation UpdateNextVisitLocation(CustomSnake checkSnake, List<List<bool>> visited, List<List<int>> distance)
        {
            CustomLocation next = new CustomLocation(checkSnake[1]);
            int min = int.MaxValue;
            for (int i = 0; i < Field.XSize; i++)
            {
                for (int j = 0; j < Field.YSize; j++)
                {
                    if (!visited[i][j] && distance[i][j] < min)
                    {
                        min = distance[i][j];
                        next = new CustomLocation(i, j);
                    }
                }
            }
            visited[next.X][next.Y] = true;
            return next;
        }

        /// <summary>
        /// Updates the distances of visitLocations neighbors
        /// </summary>
        private void UpdateAdjacentDistances(CustomSnake checkSnake, CustomSnake otherSnake, CustomLocation visitLocation,
                                             List<List<bool>> visited, List<List<int>> distance)
        {
            for (int i = 0; i < 4; i++)
            {
                CustomLocation adjacent = new CustomLocation(visitLocation.Adjacent(i, Field));
                if (IsUnvisitedFree(checkSnake, otherSnake, visited, adjacent) &&
                    distance[visitLocation.X][visitLocation.Y] + 1 < distance[adjacent.X][adjacent.Y])
                {
                    distance[adjacent.X][adjacent.Y] = distance[visitLocation.X][visitLocation.Y] + 1;
                }
            }
        }

        /// <summary>
        /// Updates the distance of checkSnakes tail based on its neighbors
        /// </summary>
        private void UpdateTailDistance(CustomSnake checkSnake, List<List<int>> distance)
        {
            int tailMin = int.MaxValue;
            for (int i = 0; i < 4; i++)
            {
                CustomLocation adjacent = new CustomLocation(checkSnake.Tail.Adjacent(i, Field));
                if (distance[adjacent.X][adjacent.Y] != int.MaxValue && distance[adjacent.X][adjacent.Y] + 1 < tailMin)
                {
                    tailMin = distance[adjacent.X][adjacent.Y] + 1;
                }
            }
            distance[checkSnake.Tail.X][checkSnake.Tail.Y] = tailMin;
        }

        /// <summary>
        /// A two dimensional bool list full of false values
        /// </summary>
        private List<List<bool>> GetVisitedDefault()
        {
            List<List<bool>> visited = new List<List<bool>>(Field.XSize);
            for (int i = 0; i < Field.XSize; i++)
            {
                List<bool> helpList = new List<bool>(Field.YSize);
                for (int j = 0; j < Field.YSize; j++)
                {
                    helpList.Add(false);
                }
                visited.Add(helpList);
            }
            return visited;
        }

        /// <summary>
        /// A two dimensional int list full of intmax values
        /// </summary>
        private List<List<int>> GetDistanceDefault()
        {
            List<List<int>> distance = new List<List<int>>(Field.XSize);
            for (int i = 0; i < Field.XSize; i++)
            {
                List<int> helpList = new List<int>(Field.YSize);
                for (int j = 0; j < Field.YSize; j++)
                {
                    helpList.Add(int.MaxValue);
                }
                distance.Add(helpList);
            }
            return distance;
        }

        /// <summary>
        /// Determines whether the distance calculation can be finished
        /// </summary>
        private bool ContinueCalculation(CustomSnake checkSnake, CustomLocation visitLocation, List<List<bool>> visited,
                                         List<List<int>> distance, CustomAppleList appleList, ulong visitCount, int limit)
        {
            if (distance[visitLocation.X][visitLocation.Y] == int.MaxValue)
            {
                return false;
            }
            if (visitCount > (ulong)limit)
            {
                if (appleList.IsEmpty())
                {
                    if (IsTailReady(checkSnake, visited))
                    {
                        return false;
                    }
                }
                else
                {
                    if (IsAppleReady(appleList, visitLocation, distance))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Determines whether the tails distance is ready to calculate 
        /// </summary>
        private bool IsTailReady(CustomSnake checkSnake, List<List<bool>> visited)
        {
            for (int i = 0; i < 4; i++)
            {
                CustomLocation adjacent = checkSnake.Tail.Adjacent(i, Field);
                if (visited[adjacent.X][adjacent.Y])
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Determines whether the calculation reached the distance when the snake would not be able to collect any current apples
        /// </summary>
        private bool IsAppleReady(CustomAppleList appleList, CustomLocation visitLocation, List<List<int>> distance)
        {
            if (appleList.MaxLife() < distance[visitLocation.X][visitLocation.Y])
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether the given location is free
        /// </summary>
        private bool IsFree(CustomSnake mySnake, CustomSnake enemySnake, CustomLocation location)
        {
            if (Field[location.X, location.Y].IsObstacle || mySnake.Contains(location) || enemySnake.Contains(location))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether the given location have not been visited and free
        /// </summary>
        private bool IsUnvisitedFree(CustomSnake checkSnake, CustomSnake otherSnake, List<List<bool>> visited, CustomLocation visitLocation)
        {
            if (!visited[visitLocation.X][visitLocation.Y] && IsFree(checkSnake, otherSnake, visitLocation))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether the given snakes head hit the wall
        /// </summary>
        private bool IsHeadInWall(CustomSnake checkSnake)
        {
            if (Field[checkSnake.Head.X, checkSnake.Head.Y].IsObstacle)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether the given snakes hit the other snake
        /// </summary>
        private bool IsHeadInOpponent(CustomSnake checkSnake, CustomSnake otherSnake)
        {
            if (otherSnake.Contains(checkSnake.Head))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether the given snake collided with itself
        /// </summary>
        private bool IsHeadInSelf(CustomSnake checkSnake)
        {
            for (int i = 1; i < checkSnake.Length; i++)
            {
                if (checkSnake.Head.IsSame(checkSnake[i]))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Determines whether the step is an instant death based on previous move direction
        /// </summary>
        private bool IsInstantDeath(int currentDirection, int lastDirection)
        {
            if (currentDirection == 0 && lastDirection == 1 ||
                currentDirection == 1 && lastDirection == 0 ||
                currentDirection == 2 && lastDirection == 3 ||
                currentDirection == 3 && lastDirection == 2)
            {
                return true;
            }
            return false;
        }
    }


    class CustomLocation
    {
        public int X { get; private set; }
        public int Y { get; private set; }

        public CustomLocation(int X, int Y)
        {
            this.X = X;
            this.Y = Y;
        }
        public CustomLocation(CustomLocation location)
        {
            X = location.X;
            Y = location.Y;
        }

        /// <summary>
        /// Determines whether the location matches the other one
        /// </summary>
        public bool IsSame(CustomLocation location2)
        {
            if (X == location2.X && Y == location2.Y)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// The adjacent location in the given direction
        /// </summary>
        public CustomLocation Adjacent(int direction, GameField Field)
        {
            CustomLocation adjacent = new CustomLocation(this);
            if (direction == 0)
            {
                if (Y == 0)
                {
                    adjacent.Y = Field.YSize - 1;
                }
                else
                {
                    adjacent.Y--;
                }
                return adjacent;
            }
            else if (direction == 1)
            {
                if (Y == Field.YSize - 1)
                {
                    adjacent.Y = 0;
                }
                else
                {
                    adjacent.Y++;
                }
                return adjacent;
            }
            else if (direction == 2)
            {
                if (X == 0)
                {
                    adjacent.X = Field.XSize - 1;
                }
                else
                {
                    adjacent.X--;
                }
                return adjacent;
            }
            else if (direction == 3)
            {
                if (X == Field.XSize - 1)
                {
                    adjacent.X = 0;
                }
                else
                {
                    adjacent.X++;
                }
                return adjacent;
            }
            else
            {
                return this;
            }
        }
    }


    class CustomApple
    {
        public CustomLocation Location { get; }

        public int X { get; }

        public int Y { get; }

        public int LifeLeft { get; private set; }

        public CustomApple(int X, int Y, int LifeLeft)
        {
            CustomLocation location = new CustomLocation(X, Y);
            Location = location;
            this.X = X;
            this.Y = Y;
            this.LifeLeft = LifeLeft;
        }

        public CustomApple(CustomApple apl2)
        {
            CustomLocation location = new CustomLocation(apl2.Location);
            Location = location;
            X = apl2.X;
            Y = apl2.Y;
            LifeLeft = apl2.LifeLeft;
        }

        /// <summary>
        /// Decreases apples LifeTime by 1
        /// </summary>
        public void DecreaseLife()
        {
            LifeLeft--;
        }
    }


    class CustomSnake
    {
        public int PlayScore { get; set; }

        public int AppleScore { get; set; }

        public int LastDirection { get; set; }

        public int TailDistance { get; set; }

        public ulong ReachableCount { get; set; }

        public bool Grow { get; private set; }

        public CustomSnake(Snake realSnake, GameField Field)
        {
            PlayScore = 0;
            AppleScore = realSnake.Score;
            TailDistance = int.MaxValue;
            ReachableCount = 0;
            Grow = true;
            locationList = new List<CustomLocation>();
            for (int i = 0; i < realSnake.Size; i++)
            {
                CustomLocation bodyPart = new CustomLocation(realSnake[i].X, realSnake[i].Y);
                locationList.Add(bodyPart);
            }
            if (realSnake.MovementDirection != null)
            {
                LastDirection = (int)realSnake.MovementDirection;
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    if (Head.Adjacent(i, Field).IsSame(this[1]))
                    {
                        if (i == 0)
                        {
                            LastDirection = 1;
                        }
                        if (i == 1)
                        {
                            LastDirection = 0;
                        }
                        if (i == 2)
                        {
                            LastDirection = 3;
                        }
                        if (i == 3)
                        {
                            LastDirection = 2;
                        }
                    }
                }
            }
        }

        public CustomSnake(CustomSnake copySnake)
        {
            PlayScore = copySnake.PlayScore;
            AppleScore = copySnake.AppleScore;
            LastDirection = copySnake.LastDirection;
            TailDistance = copySnake.TailDistance;
            ReachableCount = copySnake.ReachableCount;
            Grow = copySnake.Grow;
            locationList = new List<CustomLocation>();
            for (int i = 0; i < copySnake.Length; i++)
            {
                CustomLocation bodyPart = new CustomLocation(copySnake[i].X, copySnake[i].Y);
                locationList.Add(bodyPart);
            }
        }

        public CustomLocation this[int i]
        {
            get { return locationList[i]; }
        }

        public CustomLocation Head
        {
            get { return locationList[0]; }
        }

        public CustomLocation Tail
        {
            get { return locationList[Length - 1]; }
        }

        /// <summary>
        /// Length of the snake
        /// </summary>
        public int Length
        {
            get { return locationList.Count; }
        }

        /// <summary>
        /// Determines whether the snake contains the given location
        /// </summary>
        public bool Contains(CustomLocation location)
        {
            for (int i = 0; i < Length; i++)
            {
                if (this[i].IsSame(location))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Moves the snake in the given direction
        /// </summary>
        public void Move(int direction, GameField Field)
        {
            if (Grow)
            {
                Grow = false;
            }
            else
            {
                locationList.RemoveAt(Length - 1);
            }
            CustomLocation newHead = new CustomLocation(Head.Adjacent(direction, Field));
            locationList.Insert(0, newHead);
            LastDirection = direction;
        }

        /// <summary>
        /// Handles occasional apple consumption
        /// </summary>
        public void ConsumeApples(CustomAppleList appleList, int PointsForApple, int depth)
        {
            if (!appleList.IsEmpty())
            {
                for (int i = 0; i < appleList.Length; i++)
                {
                    if (Head.IsSame(appleList[i].Location))
                    {
                        AppleScore += PointsForApple;
                        PlayScore += PointsForApple * depth;
                        Grow = true;
                        appleList.Remove(i);
                        return;
                    }
                }
            }
        }

        private List<CustomLocation> locationList;
    }


    class CustomAppleList
    {
        public CustomApple this[int i]
        {
            get { return appleList[i]; }
        }

        public CustomAppleList(GameField Field)
        {
            appleList = new List<CustomApple>();
            for (int i = 0; i < Field.Apples.Count; i++)
            {
                CustomApple apl = new CustomApple(Field.Apples[i].Location.X, Field.Apples[i].Location.Y, Field.Apples[i].LifeLeft);
                appleList.Add(apl);
            }
        }

        public CustomAppleList(CustomAppleList appleList2)
        {
            appleList = new List<CustomApple>();
            for (int i = 0; i < appleList2.Length; i++)
            {
                CustomApple apl = new CustomApple(appleList2[i]);
                appleList.Add(apl);
            }
        }

        /// <summary>
        /// Removes an apple at the given index
        /// </summary>
        public void Remove(int index)
        {
            appleList.RemoveAt(index);
        }

        /// <summary>
        /// Length of the apple list
        /// </summary>
        public int Length
        {
            get { return appleList.Count; }
        }

        /// <summary>
        /// Determines whether there are no apples
        /// </summary>
        public bool IsEmpty()
        {
            if (Length == 0)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Decreases the LifeLeft of all apples in the list, also removes the ones that are too old
        /// </summary>
        public void UpdateLifes()
        {
            int old = 0;
            bool flag = false;
            for (int i = 0; i < Length; i++)
            {
                if (this[i].LifeLeft == 1)
                {
                    old = i;
                    flag = true;
                }
                else
                {
                    this[i].DecreaseLife();
                }
            }
            if (flag)
            {
                Remove(old);
            }
        }

        /// <summary>
        /// The oldest apples LifeLeft value
        /// </summary>
        public int MaxLife()
        {
            int max = 0;
            for (int i = 0; i < Length; i++)
            {
                if (this[i].LifeLeft > max)
                {
                    max = this[i].LifeLeft;
                }
            }
            return max;
        }

        private List<CustomApple> appleList;
    }
}
