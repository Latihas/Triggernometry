namespace Triggernometry.Expressions.Tests;

public class BasicTest : TestBase {
    public BasicTest() {
        testItems = [
            new TestItem("", ""),
            new TestItem("\nA\r\nB\rC\nD⏎E\n", "\r\nA\r\nB\r\nC\r\nD\r\nE\r\n")
        ];
    }
}