# InstantBuyOnlyCompany

## features (功能)

- When you are the host, any items you/clients purchase in the terminal will be generated directly in the ship. Purchased items are generated with a positional offset that can be manually adjusted in the config file, defaulting to 0.2

  当你是房主时，任何你/客机在终端购买的物品都会直接生成在船内。生成购买的物品时会有一个可以在config文件中手动调节的位置偏移量，默认为0.2

- Customize the list of items to be ignored for instant purchase (makes it possible to trigger dropship for some purchases) , by default all items are enabled for instant purchase, but you can customize the list of items to be ignored for purchase in config. Fill in    -1,x,x,x,x,x,x,x,     x is the number of the item, the specific correspondence is shown in the following table  (It's actually the store page that sorts the items)

  自定义忽略瞬间购买的物品列表（使得购买部分物品可以触发空投仓），默认所有物品启用瞬间购买，但你可以在config中自定义需要忽略购买的物品列表。填写方式为 -1,x,x,x,x,x,x,    x为物品对应的编号（注意逗号是英文逗号），具体对应方式如下表所示（其实就是store页面的物品排序）

```
Walkie-talkie : 0
Flashlight : 1
Shovel : 2
Lockpicker : 3
Pro-flashlight : 4
Stun grenade : 5
Boombox : 6
TZP-Inhalant : 7
Zap gun : 8
Jatpack : 9
Extension ladder : 10
Radar-booster : 11
Spray paint : 12
```

