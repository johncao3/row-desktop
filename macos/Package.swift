// swift-tools-version: 5.9
import PackageDescription
let package = Package(
    name: "ROWMac", platforms: [.macOS(.v13)],
    products: [.executable(name: "ROW", targets: ["ROW"])],
    targets: [.target(name: "RowCore"), .executableTarget(name: "ROW", dependencies: ["RowCore"]),
              .testTarget(name: "RowCoreTests", dependencies: ["RowCore"])]
)
