import XCTest
@testable import RowCore

final class CoreTests: XCTestCase {
    func testStockCodesAndPreferences() throws {
        XCTAssertEqual(try RowLogic.stockCode("600519"),"sh600519")
        XCTAssertEqual(try RowLogic.stockCode("AAPL"),"usaapl")
        XCTAssertEqual(try RowLogic.stockCode("us.INX"),"us.inx")
        XCTAssertThrowsError(try RowLogic.stockCode("../../file"))
        var settings=Preferences();settings.watches=[]
        XCTAssertEqual(try JSONDecoder().decode(Preferences.self,from:JSONEncoder().encode(settings)).validated().watches.count,0)
        settings.watches=[Watch("coin:90","BTC","加密"),Watch("coin:90","BTC","加密")]
        XCTAssertThrowsError(try settings.validated())
    }
    func testStockAndCoinParsing() throws {
        var fields=Array(repeating:"0",count:34);fields[1]="Test";fields[3]="100.5";fields[30]="20261028120000";fields[32]="-1.5"
        let stock=try FeedParser.stock(Data(("v_us.INX=\""+fields.joined(separator:"~")+"\";").utf8),code:"us.inx").first!
        XCTAssertEqual(stock.value,"100.50");XCTAssertEqual(stock.change,-1.5)
        let coin=Data("[{\"id\":\"90\",\"symbol\":\"BTC\",\"price_usd\":\"10.5\",\"percent_change_24h\":\"2\"}]".utf8)
        XCTAssertEqual(try FeedParser.coins(coin,ids:["90"]).first?.price,10.5)
        XCTAssertThrowsError(try FeedParser.coins(coin,ids:["90","80"]))
    }
    func testSportsPeriodsAndEvents() throws {
        let data=Data("""
        {"events":[{"id":"1","date":"2026-10-28T12:00:00Z","status":{"type":{"state":"in","name":"STATUS_IN_PROGRESS"},"period":5,"displayClock":"0:23"},"competitions":[{"competitors":[{"homeAway":"away","team":{"id":"2","displayName":"Chelsea"},"score":"123","linescores":[{"period":1,"value":24}]},{"homeAway":"home","team":{"id":"1","displayName":"Arsenal"},"score":"128","linescores":[{"period":1,"value":25},{"period":5,"value":8}]}],"details":[{"scoringPlay":true,"ownGoal":true,"team":{"id":"1"}},{"redCard":true,"team":{"id":"2"}}]}]}]}
        """.utf8)
        let nba=try FeedParser.sports(data,group:"NBA",league:"NBA").first!
        XCTAssertEqual(nba.value,"128 : 123");XCTAssertEqual(nba.home,"阿森纳");XCTAssertTrue(nba.details.contains("OT1  8 : —"));XCTAssertTrue(nba.details.contains("主队领先 5 分"))
        let soccer=try FeedParser.sports(data,group:"足球",league:"英超").first!
        XCTAssertTrue(soccer.details.contains("乌龙球"));XCTAssertTrue(soccer.details.contains("红牌"))
        XCTAssertEqual(try FeedParser.sports(Data("{\"events\":[]}".utf8),group:"NBA",league:"NBA").count,0)
        XCTAssertThrowsError(try FeedParser.sports(Data("{}".utf8),group:"NBA",league:"NBA"))
    }
    func testTeamScopeAndScoreChangeBaselines() {
        let now=Date();var live=RowItem("game:NBA:1","A · B","NBA","98 : 94")
        live.home="A";live.away="B";live.homeID="1";live.awayID="2";live.league="NBA";live.rank=0;live.start=now;live.day=RowLogic.day(now)
        var future=live;future.id="game:NBA:2";future.rank=1;future.start=now.addingTimeInterval(3600)
        XCTAssertEqual(RowLogic.followed(live.teamWatch(home:true),items:[future,live],now:now)?.id,live.id)
        var before=live;before.value="96 : 94"
        XCTAssertTrue(RowLogic.scoreChanged(before,live,previous:now.addingTimeInterval(-60),received:now,previousFailed:false))
        XCTAssertFalse(RowLogic.scoreChanged(before,live,previous:now.addingTimeInterval(-300),received:now,previousFailed:false))
        XCTAssertFalse(RowLogic.scoreChanged(before,live,previous:now.addingTimeInterval(-60),received:now,previousFailed:true))
        future.league="other";live.rank=2;live.day=RowLogic.day(now.addingTimeInterval(-86400))
        XCTAssertNil(RowLogic.followed(live.teamWatch(home:true),items:[future,live],now:now))
    }
}
