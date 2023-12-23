using System;
using System.Collections.Generic;
using System.Linq;
using BattleShip.BLL.Requests;
using BattleShip.BLL.Responses;
using BattleShip.BLL.Ships;

namespace BattleShip.BLL.GameLogic
{ 
    public class Board 
    // tady delame class Board tkery udelaje pro nas board s zacatycnimy koordinatama 
    {
        public const int xCoordinator = 10;
        public const int yCoordinator = 10;
        private Dictionary<Coordinate, ShotHistory> ShotHistory;
        private int _currentShipIndex;

        public Ship[] Ships { get; private set; }

        public Board()
        {  
            // ve konstrukce Board zadavame parametry jako ShotHistory , a zadavame cislo zacatecnych 5 boardu
            ShotHistory = new Dictionary<Coordinate, ShotHistory>();
            Ships = new Ship[5];
            _currentShipIndex = 0;
        }

        public FireShotResponse FireShot(Coordinate coordinate)
        {
            //  je to takova funkce ktera delaji fireshot a pak se kontroluje jestli to udelano dobre 
            var response = new FireShotResponse();
            //  ve variable response zadavame daty pro fireshot a tam je zapiseme

            // kontrolujeme jestli takova koordinata tam je?
            if (!IsValidCoordinate(coordinate))
            {
                response.ShotStatus = ShotStatus.Invalid;
                return response;
            }

            // prostredctvim Shothistory kontrolujeme si jestli uz tu koordinata jsme se zkouseli 
            if (ShotHistory.ContainsKey(coordinate))
            {
                response.ShotStatus = ShotStatus.Duplicate;
                return response;
            }

            CheckShipsForHit(coordinate, response);
            CheckForVictory(response);

            return response;            
        }

        public ShotHistory CheckCoordinate(Coordinate coordinate)
        { 
            //  v tou funkci pres klice matice v kterem mame pozice shipu kontrolujeme jestli udelali jsme dobry shot , pokud ne vratime ze shot je spatny
            if(ShotHistory.ContainsKey(coordinate))
            {
                return ShotHistory[coordinate];
            }
            else
            {
                return Responses.ShotHistory.Unknown;
            }
        }

        public ShipPlacement PlaceShip(PlaceShipRequest request)
        {
            // pres takovou konstrukce davame shipy do boardu , a zadavame tam limit na 5 shipu
            if (_currentShipIndex > 4)
                throw new Exception("You can not add another ship, 5 is the limit!");

            if (!IsValidCoordinate(request.Coordinate))
                return ShipPlacement.NotEnoughSpace;
// pres switch case davame po shipy v hre do presnych koordinatu nahore , dole, vlevo, vpravo na koordinatach
            Ship newShip = ShipCreator.CreateShip(request.ShipType);
            switch (request.Direction)
            {
                case ShipDirection.Down:
                    return PlaceShipDown(request.Coordinate, newShip);
                case ShipDirection.Up:
                    return PlaceShipUp(request.Coordinate, newShip);
                case ShipDirection.Left:
                    return PlaceShipLeft(request.Coordinate, newShip);
                default:
                    return PlaceShipRight(request.Coordinate, newShip);
            }
        }

        private void CheckForVictory(FireShotResponse response)
        {
            if (response.ShotStatus == ShotStatus.HitAndSunk)
            {
                // kontrolujeme si jestli jsme vyhraly a zabili vse 5 boardu?
                if (Ships.All(s => s.IsSunk))
                    response.ShotStatus = ShotStatus.Victory;
            }
        }

        private void CheckShipsForHit(Coordinate coordinate, FireShotResponse response)
        {
            // používá se ke kontrole, zda výstřel vypálený na souřadnici zasáhl loď. Jako parametry bere souřadnice a objekt FireShotResponse. Iteruje seznam lodí a kontroluje, zda je souřadnice v mezích lodi. Pokud ano, nastaví ShotStatus objektu odpovědi na Hit nebo HitAndSunk v závislosti na stavu lodi. Pokud výstřel mine všechny lodě, ShotStatus je nastaven na Miss. Nakonec jsou souřadnice a ShotStatus přidány do ShotHistory.
            response.ShotStatus = ShotStatus.Miss;

            foreach (var ship in Ships)
            {
                
                if (ship.IsSunk)
                    continue;

                ShotStatus status = ship.FireAtShip(coordinate);

                switch (status)
                {
                    case ShotStatus.HitAndSunk:
                        response.ShotStatus = ShotStatus.HitAndSunk;
                        response.ShipImpacted = ship.ShipName;
                        ShotHistory.Add(coordinate, Responses.ShotHistory.Hit);
                        break;
                    case ShotStatus.Hit:
                        response.ShotStatus = ShotStatus.Hit;
                        response.ShipImpacted = ship.ShipName;
                        ShotHistory.Add(coordinate, Responses.ShotHistory.Hit);
                        break;
                }

               
                if (status != ShotStatus.Miss)
                    break;
            }

            if (response.ShotStatus == ShotStatus.Miss)
            {
                ShotHistory.Add(coordinate, Responses.ShotHistory.Miss);
            }
        }

        private bool IsValidCoordinate(Coordinate coordinate)
        {
            return coordinate.XCoordinate >= 1 && coordinate.XCoordinate <= xCoordinator &&
            coordinate.YCoordinate >= 1 && coordinate.YCoordinate <= yCoordinator;
        }

        private ShipPlacement PlaceShipRight(Coordinate coordinate, Ship newShip)
        {
            // y coordinate gets bigger
            int positionIndex = 0;
            int maxY = coordinate.YCoordinate + newShip.BoardPositions.Length;

            for (int i = coordinate.YCoordinate; i < maxY; i++)
            {
                var currentCoordinate = new Coordinate(coordinate.XCoordinate, i);
                if (!IsValidCoordinate(currentCoordinate))
                    return ShipPlacement.NotEnoughSpace;

                if (OverlapsAnotherShip(currentCoordinate))
                    return ShipPlacement.Overlap;

                newShip.BoardPositions[positionIndex] = currentCoordinate;
                positionIndex++;
            }

            AddShipToBoard(newShip);
            return ShipPlacement.Ok;
        }

        private ShipPlacement PlaceShipLeft(Coordinate coordinate, Ship newShip)
        {
            // y coordinate gets smaller
            int positionIndex = 0;
            int minY = coordinate.YCoordinate - newShip.BoardPositions.Length;

            for (int i = coordinate.YCoordinate; i > minY; i--)
            {
                var currentCoordinate = new Coordinate(coordinate.XCoordinate, i);

                if (!IsValidCoordinate(currentCoordinate))
                    return ShipPlacement.NotEnoughSpace;

                if (OverlapsAnotherShip(currentCoordinate))
                    return ShipPlacement.Overlap;

                newShip.BoardPositions[positionIndex] = currentCoordinate;
                positionIndex++;
            }

            AddShipToBoard(newShip);
            return ShipPlacement.Ok;
        }

        private ShipPlacement PlaceShipUp(Coordinate coordinate, Ship newShip)
        {
            // x coordinate gets smaller
            int positionIndex = 0;
            int minX = coordinate.XCoordinate - newShip.BoardPositions.Length;

            for (int i = coordinate.XCoordinate; i > minX; i--)
            {
                var currentCoordinate = new Coordinate(i, coordinate.YCoordinate);

                if (!IsValidCoordinate(currentCoordinate))
                    return ShipPlacement.NotEnoughSpace;

                if (OverlapsAnotherShip(currentCoordinate))
                    return ShipPlacement.Overlap;

                newShip.BoardPositions[positionIndex] = currentCoordinate;
                positionIndex++;
            }

            AddShipToBoard(newShip);
            return ShipPlacement.Ok;
        }

        private ShipPlacement PlaceShipDown(Coordinate coordinate, Ship newShip)
        {
            // y coordinate gets bigger
            int positionIndex = 0;
            int maxX = coordinate.XCoordinate + newShip.BoardPositions.Length;

            for (int i = coordinate.XCoordinate; i < maxX; i++)
            {
                var currentCoordinate = new Coordinate(i, coordinate.YCoordinate);

                if (!IsValidCoordinate(currentCoordinate))
                    return ShipPlacement.NotEnoughSpace;

                if (OverlapsAnotherShip(currentCoordinate))
                    return ShipPlacement.Overlap;

                newShip.BoardPositions[positionIndex] = currentCoordinate;
                positionIndex++;
            }

            AddShipToBoard(newShip);
            return ShipPlacement.Ok;
        }

        private void AddShipToBoard(Ship newShip)
        {
            Ships[_currentShipIndex] = newShip;
            _currentShipIndex++;
        }

        private bool OverlapsAnotherShip(Coordinate coordinate)
        {
            foreach (var ship in Ships)
            {
                if (ship != null)
                {
                    if (ship.BoardPositions.Contains(coordinate))
                        return true;
                }
            }

            return false;
        }
    }
}
