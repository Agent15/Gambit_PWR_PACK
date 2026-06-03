using System.Collections.Generic;
using Gambonanza.EnemyThreatOverlay;
using NUnit.Framework;

namespace Gambonanza.EnemyThreatOverlay.Tests
{
    public sealed class ThreatOverlayCoreTests
    {
        [Test]
        public void ControllerActivatesOnlyWhileMiddleMouseIsHeldInGame()
        {
            var input = new FakeInput();
            var state = new FakeState { CurrentState = ThreatOverlayGameState.InGame };
            var tile = new FakeTile();
            var visuals = new FakeVisuals();
            var controller = CreateController(input, state, new[] { FakePiece.EnemyNonPawn(tile) }, visuals);

            controller.Tick(0f);
            Assert.False(controller.IsActive);
            Assert.AreEqual(0, visuals.ShowEndangerCount(tile));

            input.Held = true;
            controller.Tick(0.1f);
            Assert.True(controller.IsActive);
            Assert.AreEqual(1, visuals.ShowEndangerCount(tile));

            input.Held = false;
            controller.Tick(0.2f);
            Assert.False(controller.IsActive);
            Assert.AreEqual(1, visuals.HideEndangerCount(tile));
        }

        [Test]
        public void ControllerHidesWhenStateLeavesInGame()
        {
            var input = new FakeInput { Held = true };
            var state = new FakeState { CurrentState = ThreatOverlayGameState.InGame };
            var tile = new FakeTile();
            var visuals = new FakeVisuals();
            var controller = CreateController(input, state, new[] { FakePiece.EnemyNonPawn(tile) }, visuals);

            controller.Tick(0f);
            state.CurrentState = ThreatOverlayGameState.Other;
            controller.Tick(0.1f);

            Assert.False(controller.IsActive);
            Assert.AreEqual(1, visuals.HideEndangerCount(tile));
        }

        [Test]
        public void ControllerActivatesWhileMiddleMouseIsHeldInBoardPlacement()
        {
            var input = new FakeInput { Held = true };
            var state = new FakeState { CurrentState = ThreatOverlayGameState.BoardPlacement };
            var tile = new FakeTile();
            var visuals = new FakeVisuals();
            var controller = CreateController(input, state, new[] { FakePiece.EnemyNonPawn(tile) }, visuals);

            controller.Tick(0f);

            Assert.True(controller.IsActive);
            Assert.AreEqual(1, visuals.ShowEndangerCount(tile));
        }

        [Test]
        public void PawnUsesOnlyDiagonalEatTiles()
        {
            var forward = new FakeTile();
            var diagonal = new FakeTile();
            var piece = FakePiece.EnemyPawn(new[] { forward }, new[] { diagonal });
            var collector = new ThreatCollector(new FakePieceSource(new[] { piece }));

            var result = collector.Collect();

            Assert.AreEqual(0, Count(result.EndangerTiles, forward));
            Assert.AreEqual(1, Count(result.EndangerTiles, diagonal));
        }

        [Test]
        public void NonPawnUsesThreatTiles()
        {
            var tile = new FakeTile();
            var collector = new ThreatCollector(new FakePieceSource(new[] { FakePiece.EnemyNonPawn(tile) }));

            var result = collector.Collect();

            Assert.AreEqual(1, Count(result.EndangerTiles, tile));
        }

        [Test]
        public void DuplicateTargetsAreShownAndHiddenOnce()
        {
            var input = new FakeInput { Held = true };
            var state = new FakeState { CurrentState = ThreatOverlayGameState.InGame };
            var tile = new FakeTile();
            var visuals = new FakeVisuals();
            var pieces = new[] { FakePiece.EnemyNonPawn(tile), FakePiece.EnemyNonPawn(tile) };
            var controller = CreateController(input, state, pieces, visuals);

            controller.Tick(0f);
            controller.Hide();

            Assert.AreEqual(1, visuals.ShowEndangerCount(tile));
            Assert.AreEqual(1, visuals.HideEndangerCount(tile));
        }

        [Test]
        public void HideClearsDisplayedTilesForTeardown()
        {
            var input = new FakeInput { Held = true };
            var state = new FakeState { CurrentState = ThreatOverlayGameState.InGame };
            var empty = new FakeTile();
            var occupied = new FakeTile();
            var visuals = new FakeVisuals();
            var controller = CreateController(input, state, new[] { FakePiece.EnemyNonPawn(new[] { empty }, new[] { occupied }) }, visuals);

            controller.Tick(0f);
            controller.Hide();

            Assert.False(controller.IsActive);
            Assert.AreEqual(1, visuals.HideEndangerCount(empty));
            Assert.AreEqual(1, visuals.HideEndangerCount(occupied));
        }

        [Test]
        public void BlockedControlTargetsUseTheSameEndangerVisualsAsEmptyTargets()
        {
            var empty = new FakeTile();
            var occupied = new FakeTile();
            var input = new FakeInput { Held = true };
            var state = new FakeState { CurrentState = ThreatOverlayGameState.InGame };
            var visuals = new FakeVisuals();
            var controller = CreateController(input, state, new[] { FakePiece.EnemyNonPawn(new[] { empty }, new[] { occupied }) }, visuals);

            controller.Tick(0f);
            controller.Hide();

            Assert.AreEqual(1, visuals.ShowEndangerCount(empty));
            Assert.AreEqual(1, visuals.HideEndangerCount(empty));
            Assert.AreEqual(1, visuals.ShowEndangerCount(occupied));
            Assert.AreEqual(1, visuals.HideEndangerCount(occupied));
        }

        [Test]
        public void PawnProtectedTargetsUseTheSameEndangerVisuals()
        {
            var protectedEnemy = new FakeTile();
            var input = new FakeInput { Held = true };
            var state = new FakeState { CurrentState = ThreatOverlayGameState.InGame };
            var visuals = new FakeVisuals();
            var controller = CreateController(input, state, new[] { FakePiece.EnemyPawn(new IThreatOverlayTile[0], new[] { protectedEnemy }) }, visuals);

            controller.Tick(0f);
            controller.Hide();

            Assert.AreEqual(1, visuals.ShowEndangerCount(protectedEnemy));
            Assert.AreEqual(1, visuals.HideEndangerCount(protectedEnemy));
        }

        [Test]
        public void InvalidPiecesAndInvalidTilesAreIgnored()
        {
            var valid = new FakeTile();
            var stock = new FakeTile { IsStock = true };
            var fallen = new FakeTile { HasFell = true };
            var deadEnemy = FakePiece.EnemyNonPawn(new FakeTile());
            deadEnemy.IsDead = true;
            var disabledEnemy = FakePiece.EnemyNonPawn(new FakeTile());
            disabledEnemy.IsEnabled = false;
            var stockEnemy = FakePiece.EnemyNonPawn(new FakeTile());
            stockEnemy.InStock = true;
            var player = FakePiece.PlayerNonPawn(new FakeTile());
            var goodEnemy = FakePiece.EnemyNonPawn(valid, stock, fallen, null);

            var collector = new ThreatCollector(new FakePieceSource(new IThreatOverlayPiece[] { null, deadEnemy, disabledEnemy, stockEnemy, player, goodEnemy }));
            var result = collector.Collect();

            Assert.AreEqual(1, Count(result.EndangerTiles, valid));
            Assert.AreEqual(0, Count(result.EndangerTiles, stock));
            Assert.AreEqual(0, Count(result.EndangerTiles, fallen));
        }

        private static ThreatOverlayController CreateController(
            FakeInput input,
            FakeState state,
            IEnumerable<IThreatOverlayPiece> pieces,
            FakeVisuals visuals)
        {
            return new ThreatOverlayController(
                input,
                state,
                new ThreatCollector(new FakePieceSource(pieces)),
                visuals,
                new FakeLog(),
                0.1f);
        }

        private static int Count(IEnumerable<IThreatOverlayTile> tiles, IThreatOverlayTile expected)
        {
            var count = 0;
            foreach (var tile in tiles)
            {
                if (ReferenceEquals(tile, expected)) count++;
            }
            return count;
        }

        private sealed class FakeInput : IThreatOverlayInput
        {
            public bool Held;
            public bool IsMiddleMouseHeld => Held;
        }

        private sealed class FakeState : IThreatOverlayGameStateSource
        {
            public ThreatOverlayGameState CurrentState { get; set; }
        }

        private sealed class FakeTile : IThreatOverlayTile
        {
            public bool IsStock { get; set; }
            public bool HasFell { get; set; }
        }

        private sealed class FakePiece : IThreatOverlayPiece
        {
            private readonly IEnumerable<IThreatOverlayTile> _threatTiles;
            private readonly IEnumerable<IThreatOverlayTile> _occupiedTiles;
            private readonly IEnumerable<IThreatOverlayTile> _eatTiles;

            private FakePiece(
                bool enemy,
                bool pawn,
                IEnumerable<IThreatOverlayTile> threatTiles,
                IEnumerable<IThreatOverlayTile> occupiedTiles,
                IEnumerable<IThreatOverlayTile> eatTiles)
            {
                IsEnemy = enemy;
                IsPawn = pawn;
                IsEnabled = true;
                _threatTiles = threatTiles;
                _occupiedTiles = occupiedTiles;
                _eatTiles = eatTiles;
            }

            public bool IsEnemy { get; set; }
            public bool IsPawn { get; set; }
            public bool IsDead { get; set; }
            public bool IsEnabled { get; set; }
            public bool InStock { get; set; }

            public static FakePiece EnemyNonPawn(params IThreatOverlayTile[] threatTiles)
            {
                return EnemyNonPawn(threatTiles, new IThreatOverlayTile[0]);
            }

            public static FakePiece EnemyNonPawn(IThreatOverlayTile[] threatTiles, IThreatOverlayTile[] occupiedTiles)
            {
                return new FakePiece(true, false, threatTiles, occupiedTiles, new IThreatOverlayTile[0]);
            }

            public static FakePiece PlayerNonPawn(params IThreatOverlayTile[] threatTiles)
            {
                return new FakePiece(false, false, threatTiles, new IThreatOverlayTile[0], new IThreatOverlayTile[0]);
            }

            public static FakePiece EnemyPawn(IEnumerable<IThreatOverlayTile> threatTiles, IEnumerable<IThreatOverlayTile> eatTiles)
            {
                return new FakePiece(true, true, threatTiles, new IThreatOverlayTile[0], eatTiles);
            }

            public IEnumerable<IThreatOverlayTile> GetThreatTiles()
            {
                return _threatTiles;
            }

            public IEnumerable<IThreatOverlayTile> GetOccupiedTiles()
            {
                return _occupiedTiles;
            }

            public IEnumerable<IThreatOverlayTile> GetPawnEatTiles()
            {
                return _eatTiles;
            }
        }

        private sealed class FakePieceSource : IThreatOverlayPieceSource
        {
            private readonly IEnumerable<IThreatOverlayPiece> _pieces;

            public FakePieceSource(IEnumerable<IThreatOverlayPiece> pieces)
            {
                _pieces = pieces;
            }

            public IEnumerable<IThreatOverlayPiece> GetPieces()
            {
                return _pieces;
            }
        }

        private sealed class FakeVisuals : IThreatOverlayTileVisuals
        {
            private readonly Dictionary<IThreatOverlayTile, int> _showEndanger = new Dictionary<IThreatOverlayTile, int>();
            private readonly Dictionary<IThreatOverlayTile, int> _hideEndanger = new Dictionary<IThreatOverlayTile, int>();

            public void ShowEndanger(IThreatOverlayTile tile) { Increment(_showEndanger, tile); }
            public void HideEndanger(IThreatOverlayTile tile) { Increment(_hideEndanger, tile); }

            public int ShowEndangerCount(IThreatOverlayTile tile) { return Get(_showEndanger, tile); }
            public int HideEndangerCount(IThreatOverlayTile tile) { return Get(_hideEndanger, tile); }

            private static void Increment(Dictionary<IThreatOverlayTile, int> calls, IThreatOverlayTile tile)
            {
                calls[tile] = Get(calls, tile) + 1;
            }

            private static int Get(Dictionary<IThreatOverlayTile, int> calls, IThreatOverlayTile tile)
            {
                return calls.TryGetValue(tile, out var count) ? count : 0;
            }
        }

        private sealed class FakeLog : IThreatOverlayLog
        {
            public void Line(string message) { }
        }
    }
}
