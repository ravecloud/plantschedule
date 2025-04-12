from simulator import *

m1 = Resource("M1", 1.0, 0.5, 0.5)
m2 = Resource("M2", 1.0)


op = Operation([m1,m1],[],[],[])
r1 = Recipe([op])

o1 = Order(r1)
