import { createClient, defineScript } from 'redis';

const KEY = `account1/balance`;
const DEFAULT_BALANCE = 100;

const updatebalanceScript = `
local balance = redis.call('GET', KEYS[1]);

if(balance - ARGV[1] >= 0) then
    return redis.call('DECRBY', KEYS[1], ARGV[1]);
end

return balance;
`;

const client = createClient({
    url: `redis://${process.env.ENDPOINT}:${parseInt(process.env.PORT || "6379")}`,
    scripts: {
        updateBalance: defineScript({
            NUMBER_OF_KEYS: 1,
            SCRIPT: updatebalanceScript,
            transformArguments(key, charge) {
                return [key, charge.toString()];
            }
        })
    }
});

await client.connect();

export const chargeRequestRedis = async (input) => {
    const charges = getCharges();


    var remainingBalance = await client.updateBalance(KEY, charges);

    const isAuthorized = remainingBalance >= 0;

    if (!isAuthorized) {
        charges = 0;
    }

    return {
        remainingBalance,
        charges,
        isAuthorized,
    };
};

export const resetRedis = async () => {
    await client.set(KEY, DEFAULT_BALANCE);
};

function getCharges() {
    return DEFAULT_BALANCE / 20;
}