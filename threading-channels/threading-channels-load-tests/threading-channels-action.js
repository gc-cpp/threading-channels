import http from 'k6/http';
import { sleep } from 'k6';
import { randomIntBetween } from 'https://jslib.k6.io/k6-utils/1.2.0/index.js';

const url = 'http://localhost:1012/channel';

export const options = {
    thresholds: {
        http_req_failed: ['rate < 0.01'],
        http_req_duration: ['p(95) < 100'],
    },
    scenarios: {
      load: {
        executor: 'constant-arrival-rate',
        duration: '15m', // total duration
        preAllocatedVUs: 150, // to allocate runtime resources
        rate: 100, // number of constant iterations given `timeUnit`
        timeUnit: '10s',
      },
      stress: {
        executor: "ramping-arrival-rate",
        preAllocatedVUs: 250,
        timeUnit: "10s",
        startRate: 50,
        stages: [
          { duration: "1m", target: 25 }, // below normal load
          { duration: "2m", target: 150 },
          { duration: "2m", target: 250 }, // normal load
          { duration: "2m", target: 400 },
          { duration: "2m", target: 720 }, // around the breaking point
          { duration: "2m", target: 100 },
          { duration: "2m", target: 20 }, // beyond the breaking point
          { duration: "1m", target: 0 }, // scale down. Recovery stage.
        ],
      },
      soak: {
        stages: [
            { duration: '2m', target: 200 },
            { duration: '56m', target: 200 },
            { duration: '2m', target: 0 },
          ],
      }
    },
};

const subscribeUser = 1;
const unsubscribeUser = 10;

var userIds = [];

function uuidv4() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'
    .replace(/[xy]/g, function (c) {
        const r = Math.random() * 16 | 0, 
            v = c == 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}

export default function () {
    const params = {
        headers: {
            'Content-Type': 'application/json',
        },
    };

    var variant = randomIntBetween(subscribeUser, unsubscribeUser);

    if (userIds.length === 0 || variant === subscribeUser) {
        var userId = uuidv4();
        http.post(`${url}/subscribe/${userId}`, null, params, {
            tags: { name: 'subscribe' },
          });
        userIds.push(userId);
    } else if (variant === unsubscribeUser) {
        var userId = userIds[randomIntBetween(0, userIds.length - 1)];
        http.post(`${url}/unsubscribe/${userId}`, null, params, {
            tags: { name: 'unsubscribe' },
          });
        var index = userIds.indexOf(userId);
        userIds.splice(index, 1);
    } else {
        var userId = userIds[randomIntBetween(0, userIds.length - 1)];
        const payload = JSON.stringify({
            userId: userId,
            action: uuidv4(),
          });
        http.post(`${url}/action`, payload, params, {
            tags: { name: 'action' },
          });
    }

    sleep(1);
}