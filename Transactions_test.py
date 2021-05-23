#Transaction.py
import Signatures
#Signatures.sign
#Signatures.verify

class Tx:
    inputs = None
    outputs =None
    sigs = None
    reqd = None
    def __init__(self):
        self.inputs = []
        self.outputs = []
        self.sigs = []
        self.reqd = []
    def add_input(self, from_addr, amount):
        pass
    def add_output(self, to_addr, amount):
        pass
    def add_reqd(self, addr):
        pass
    def sign(self, private):
        pass        
    def is_valid(self):
        return False

if __name__ == "__main__":
    pr1, pu1 = Signatures.generate_keys()
    pr2, pu2 = Signatures.generate_keys()
    pr3, pu3 = Signatures.generate_keys()
    pr4, pu4 = Signatures.generate_keys()

    Tx1 = Tx()
    Tx1.add_input(pu1, 1)
    Tx1.add_output(pu2, 1)
    Tx1.sign(pr1)
    if Tx1.is_valid():
        print("Success! Tx is valid")
    else:
        print("ERROR! Tx is invalid")

    Tx2 = Tx()
    Tx2.add_input(pu1, 2)
    Tx2.add_output(pu2, 1)
    Tx2.add_output(pu3, 1)
    Tx2.sign(pr1)

    Tx3 = Tx()
    Tx3.add_input(pu3, 1.2)
    Tx3.add_output(pu1, 1.1)
    Tx3.add_reqd(pu4)
    Tx3.sign(pr3)
    Tx3.sign(pr4)

    for t in [Tx1, Tx2, Tx3]:
        if t.is_valid():
            print("Success! Tx is valid")
        else:
            print("ERROR! Tx is invalid")

    # Wrong signatures
    Tx4 = Tx()
    Tx4.add_input(pu1, 1)
    Tx4.add_output(pu2, 1)
    Tx4.sign(pr2)

    # Escrow Tx not signed by the arbiter
    Tx5 = Tx()
    Tx5.add_input(pu3, 1.2)
    Tx5.add_output(pu1, 1.1)
    Tx5.add_reqd(pu4)
    Tx5.sign(pr3)

    # Two input addrs, signed by one
    Tx6 = Tx()
    Tx6.add_input(pu3, 1)
    Tx6.add_input(pu4, 0.1)
    Tx6.add_output(pu1, 1.1)
    Tx6.sign(pr3)

    # Outputs exceed inputs
    Tx7 = Tx()
    Tx7.add_input(pu4, 1.2)
    Tx7.add_output(pu1, 1)
    Tx7.add_output(pu2, 2)
    Tx7.sign(pr4)

    # Negative values
    Tx8 = Tx()
    Tx8.add_input(pu2, -1)
    Tx8.add_output(pu1, -1)
    Tx8.sign(pr2)

    # Modified Tx
    Tx9 = Tx()
    Tx9.add_input(pu1, 1)
    Tx9.add_output(pu2, 1)
    Tx9.sign(pr1)
    # outputs = [(pu2,1)]
    # change to [(pu3,1)]
    Tx9.outputs[0] = (pu3,1)
    

    for t in [Tx4, Tx5, Tx6, Tx7, Tx8, Tx9]:
        if t.is_valid():
            print("ERROR! Bad Tx is valid")
        else:
            print("Success! Bad Tx is invalid")
    
        




    
    
        
