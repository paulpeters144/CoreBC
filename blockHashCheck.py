import hashlib
from binascii import unhexlify, hexlify
version_field = "01000000"
prev_block_hash = "81cd02ab7e569e8bcd9317e2fe99f2de44d49ab2b8851ba4a308000000000000"
merkle_root = "e320b6c2fffc8d750423db8b1eb942ae710e951ed797f7affc8892b0f1fc122b"
current_timestamp = "c7f5d74d"
difficulty = "f2b9441a"
nonce = "42a14695" # 9546A142
header_hex = (version_field + prev_block_hash + merkle_root + current_timestamp + difficulty + nonce)
# header_hex = ("01000000" + 
# "81cd02ab7e569e8bcd9317e2fe99f2de44d49ab2b8851ba4a308000000000000" +
# "e320b6c2fffc8d750423db8b1eb942ae710e951ed797f7affc8892b0f1fc122b" +
# "c7f5d74d" +
# "f2b9441a" +
# "42a14695")

header_bin = unhexlify(header_hex)
hash = hashlib.sha256(hashlib.sha256(header_bin).digest()).digest()
test1 = hexlify(hash).decode("utf-8")
print(test1)
test2 = hexlify(hash[::-1]).decode("utf-8")
print(test2)


# Block 125552
